-- =====================================================================
-- KalsadaWatch database schema (Supabase / Postgres)
--
-- How to run: Supabase Dashboard -> SQL Editor -> New query -> paste this
-- whole file -> Run.
--
-- The script can be run again: it drops and recreates every KalsadaWatch
-- table, so running it again DELETES all data and restores the sample rows.
-- =====================================================================

drop view if exists public.report_dashboard;
drop view if exists public.approved_reviews;
drop view if exists public.hazard_report_status_counts;
drop table if exists public.report_photos, public.citizen_reviews, public.status_updates, public.hazard_reports, public.districts cascade;
drop function if exists public.set_hazard_report_resolved_at();
drop function if exists public.log_hazard_report_status();
drop sequence if exists public.hazard_ticket_seq;


-- ---------------------------------------------------------------------
-- Tables
-- ---------------------------------------------------------------------

-- Operational areas. Every district dropdown in the app reads this table.
create table public.districts (
  id   bigint generated always as identity primary key,
  name text not null unique check (length(trim(name)) > 0),
  city text not null default 'Manila'
);

-- New tickets continue from KW-9000. The sample tickets below use older numbers.
create sequence public.hazard_ticket_seq start 9000;

-- One row per road hazard a citizen reports. Each row is a repair ticket.
-- Storage pattern: SOFT DELETE. A withdrawn report gets a deleted_at date
-- instead of being removed. Hard deletes are not granted to the app at all.
create table public.hazard_reports (
  id                bigint generated always as identity primary key,
  ticket_number     text not null unique default ('KW-' || nextval('public.hazard_ticket_seq')),
  district_id       bigint not null references public.districts (id),
  reporter_id       bigint,  -- becomes a foreign key when a users table is added
  location_landmark text not null check (length(trim(location_landmark)) between 3 and 200),
  latitude          double precision check (latitude between -90 and 90),
  longitude         double precision check (longitude between -180 and 180),
  hazard_type       text not null check (hazard_type in ('deep_pothole', 'alligator_cracks', 'drain_collapse', 'shoulder_cavity')),
  severity          text not null check (severity in ('low', 'medium', 'high')),
  -- Lets the app sort by priority in the database (text would sort high, low, medium).
  severity_rank     smallint generated always as (case severity when 'high' then 3 when 'medium' then 2 else 1 end) stored,
  status            text not null default 'submitted'
                    check (status in ('submitted', 'under_inspection', 'under_construction', 'repaired', 'rejected')),
  status_note       text check (length(status_note) <= 500),  -- note that goes with the latest status change
  description       text not null default '' check (length(description) <= 1000),
  submitted_at      timestamptz not null default now(),
  inspection_due_at timestamptz not null default now() + interval '48 hours',  -- the 48-hour inspection promise
  resolved_at       timestamptz,  -- filled in by a trigger when the status becomes 'repaired'
  deleted_at        timestamptz,  -- soft delete
  check ((latitude is null) = (longitude is null))
);

-- The Verify page filters by these columns. Partial indexes skip withdrawn reports.
create index hazard_reports_status_idx   on public.hazard_reports (status)      where deleted_at is null;
create index hazard_reports_district_idx on public.hazard_reports (district_id) where deleted_at is null;

-- Storage pattern: HISTORY TABLE. One row per status change, written by the
-- trigger below. The app can read it but never edit or delete it.
create table public.status_updates (
  id               bigint generated always as identity primary key,
  hazard_report_id bigint not null references public.hazard_reports (id),
  status           text not null
                   check (status in ('submitted', 'under_inspection', 'under_construction', 'repaired', 'rejected')),
  note             text,
  updated_by_id    bigint,  -- becomes a foreign key when a users table is added
  updated_at       timestamptz not null default now()
);

create index status_updates_report_idx on public.status_updates (hazard_report_id, updated_at);

-- Photos attached to a report. The image file lives in Supabase Storage
-- (bucket "report-photos"); this table records which report it belongs to.
create table public.report_photos (
  id               bigint generated always as identity primary key,
  hazard_report_id bigint not null references public.hazard_reports (id),
  storage_path     text not null unique,  -- e.g. KW-9002/3f2a....jpg inside the bucket
  image_url        text not null,         -- public URL the pages show
  photo_type       text not null default 'evidence' check (photo_type in ('evidence', 'progress', 'completion')),
  uploaded_at      timestamptz not null default now()
);

create index report_photos_report_idx on public.report_photos (hazard_report_id);

-- Resident ratings and comments for the homepage feedback section.
-- New reviews wait for moderation (is_approved = false). Removing one is a soft delete.
create table public.citizen_reviews (
  id            bigint generated always as identity primary key,
  display_name  text not null default 'Anonymous resident' check (length(trim(display_name)) between 1 and 40),
  area          text not null check (length(trim(area)) between 1 and 60),
  rating        smallint check (rating between 1 and 5),
  comment       text check (length(comment) <= 500),
  ticket_number text references public.hazard_reports (ticket_number),
  is_approved   boolean not null default false,
  posted_at     timestamptz not null default now(),
  deleted_at    timestamptz,
  check (rating is not null or comment is not null)  -- a review needs at least one of the two
);


-- ---------------------------------------------------------------------
-- Triggers
-- ---------------------------------------------------------------------

-- Keeps resolved_at in step with the status.
create function public.set_hazard_report_resolved_at()
returns trigger
language plpgsql
set search_path = ''
as $$
begin
  if new.status = 'repaired' then
    if tg_op = 'INSERT' or old.status is distinct from 'repaired' then
      new.resolved_at := now();
    end if;
  else
    new.resolved_at := null;
  end if;
  return new;
end;
$$;

create trigger hazard_reports_set_resolved_at
before insert or update of status on public.hazard_reports
for each row execute function public.set_hazard_report_resolved_at();

-- Writes the history row. The app changes a status with ONE update request
-- (status + status_note) and the database records the change itself.
-- "security definer" lets the trigger write to status_updates even though
-- the app is only allowed to read that table.
create function public.log_hazard_report_status()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
  if tg_op = 'INSERT' or new.status is distinct from old.status then
    insert into public.status_updates (hazard_report_id, status, note)
    values (new.id, new.status, new.status_note);
  end if;
  return null;
end;
$$;

create trigger hazard_reports_log_status
after insert or update of status on public.hazard_reports
for each row execute function public.log_hazard_report_status();


-- ---------------------------------------------------------------------
-- Views (read-only shortcuts the app queries like tables)
-- security_invoker = true makes each view follow the caller's permissions.
-- ---------------------------------------------------------------------

-- Status counts for the Verify page badges: one small request instead of loading every report.
create view public.hazard_report_status_counts
with (security_invoker = true) as
select status, count(*)::int as total
from public.hazard_reports
where deleted_at is null
group by status;

-- Approved, not-removed reviews, with whether the linked report has been repaired.
create view public.approved_reviews
with (security_invoker = true) as
select r.id,
       r.display_name,
       r.area,
       r.posted_at,
       r.rating,
       r.comment,
       r.ticket_number,
       coalesce(h.status = 'repaired', false) as is_resolved
from public.citizen_reviews r
left join public.hazard_reports h on h.ticket_number = r.ticket_number
where r.is_approved and r.deleted_at is null;

-- One row of live numbers for the homepage: one small request instead of
-- loading every report. "Response" = time from submission to the first
-- status change after 'submitted' (taken from the history table).
create view public.report_dashboard
with (security_invoker = true) as
with active as (
  select * from public.hazard_reports where deleted_at is null
),
first_response as (
  select a.id, min(s.updated_at) - a.submitted_at as response_time
  from active a
  join public.status_updates s on s.hazard_report_id = a.id and s.status <> 'submitted'
  group by a.id, a.submitted_at
),
busiest as (
  select d.name, count(*)::int as open_reports
  from active a
  join public.districts d on d.id = a.district_id
  where a.status in ('submitted', 'under_inspection', 'under_construction')
  group by d.name
  order by count(*) desc, d.name
  limit 1
)
select
  (select count(*) from active)::int                                                 as total_reports,
  (select count(*) from active where status = 'repaired')::int                       as repaired_reports,
  (select count(*) from active where status in ('submitted', 'under_inspection'))::int as inspection_reports,
  (select count(*) from active where status = 'under_construction')::int             as construction_reports,
  (select count(*) from public.districts)::int                                       as district_count,
  (select round((extract(epoch from avg(response_time)) / 3600)::numeric, 1)
     from first_response)::double precision                                          as avg_response_hours,
  (select max(updated_at) from public.status_updates)                                as last_activity_at,
  (select name from busiest)                                                         as busiest_district,
  coalesce((select open_reports from busiest), 0)                                    as busiest_district_open;


-- ---------------------------------------------------------------------
-- Locks (slide 45): grants say WHAT the app may do, policies say WHICH ROWS.
-- These open policies are for class only. A real launch needs sign-in and
-- stricter rules.
-- ---------------------------------------------------------------------

grant select                 on public.districts       to anon;
grant select, insert, update on public.hazard_reports  to anon;  -- no DELETE: withdrawing is a soft delete
grant usage                  on sequence public.hazard_ticket_seq to anon;
grant select                 on public.status_updates  to anon;  -- history is read-only for the app
grant select, insert, update on public.citizen_reviews to anon;
grant select on public.hazard_report_status_counts, public.approved_reviews, public.report_dashboard to anon;

alter table public.districts       enable row level security;
alter table public.hazard_reports  enable row level security;
alter table public.status_updates  enable row level security;
alter table public.citizen_reviews enable row level security;

create policy "read_all" on public.districts for select to anon using (true);

create policy "read_all"   on public.hazard_reports for select to anon using (true);
create policy "insert_new" on public.hazard_reports for insert to anon
  with check (status = 'submitted' and deleted_at is null);  -- new reports always start as submitted
create policy "update_all" on public.hazard_reports for update to anon using (true) with check (true);

create policy "read_all" on public.status_updates for select to anon using (true);

grant select, insert on public.report_photos to anon;  -- photos are never edited or deleted by the app
alter table public.report_photos enable row level security;
create policy "read_all"         on public.report_photos for select to anon using (true);
create policy "insert_evidence"  on public.report_photos for insert to anon
  with check (photo_type = 'evidence');  -- citizens add evidence; progress/completion photos come from crews later

create policy "read_all"       on public.citizen_reviews for select to anon using (true);
create policy "insert_pending" on public.citizen_reviews for insert to anon
  with check (is_approved = false and deleted_at is null);  -- new reviews wait for moderation
create policy "update_all"     on public.citizen_reviews for update to anon using (true) with check (true);


-- ---------------------------------------------------------------------
-- Photo storage (Supabase Storage)
-- ---------------------------------------------------------------------

-- Storage bucket for the image files. Public = anyone with the link can view a photo.
-- Limits: 10 MB per file, JPEG/PNG/WebP only.
insert into storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
values ('report-photos', 'report-photos', true, 10485760, array['image/jpeg', 'image/png', 'image/webp'])
on conflict (id) do update
  set public = excluded.public,
      file_size_limit = excluded.file_size_limit,
      allowed_mime_types = excluded.allowed_mime_types;

-- The app may add new files to this bucket, but not replace or delete them.
drop policy if exists "report_photos_upload" on storage.objects;
create policy "report_photos_upload" on storage.objects for insert to anon
  with check (bucket_id = 'report-photos');


-- ---------------------------------------------------------------------
-- Sample data (the reports that used to be hardcoded in Verify.razor)
-- ---------------------------------------------------------------------

insert into public.districts (name) values
  ('Central District'),
  ('North District'),
  ('South Urban Corridor'),
  ('East Industrial Zone'),
  ('West Municipal Area');

insert into public.hazard_reports
  (ticket_number, district_id, location_landmark, latitude, longitude, hazard_type, severity,
   status, status_note, description, submitted_at, inspection_due_at)
select v.ticket, d.id, v.landmark, v.lat, v.lng, v.hazard, v.severity,
       'submitted', 'Report received from citizen.', v.description,
       now() - v.age, now() - v.age + interval '48 hours'
from (values
  ('KW-8921', 'Central District',     '18 Market Street / Central Blvd',       14.5995, 120.9842, 'deep_pothole',     'high',   '12-inch deep pothole across the inner lane.',                     interval '2 hours'),
  ('KW-8910', 'South Urban Corridor', 'South Expressway Service Road',         14.5820, 120.9950, 'alligator_cracks', 'medium', 'Degraded asphalt along about 40 meters of the service road.',     interval '1 day'),
  ('KW-8924', 'North District',       '45 Northway Highway near Overpass',     14.6150, 120.9890, 'alligator_cracks', 'high',   'Alligator cracking with a cavity forming near the overpass.',     interval '5 hours'),
  ('KW-8930', 'East Industrial Zone', 'Corner Industrial Ave & 8th St',        14.5930, 121.0180, 'drain_collapse',   'high',   'Collapsed manhole with a cavity on the road shoulder.',           interval '30 minutes'),
  ('KW-8902', 'Central District',     'Rizal Avenue in front of Civic Center', 14.6040, 120.9820, 'alligator_cracks', 'low',    'Worn pavement and faded markings at the pedestrian crossing.',    interval '3 days'),
  ('KW-8935', 'West Municipal Area',  'West Coastal Access Road km 12',        14.5880, 120.9680, 'shoulder_cavity',  'medium', 'The road shoulder has sunk several inches.',                      interval '1 hour'),
  ('KW-8898', 'North District',       'Quezon Blvd Intersection',              14.6100, 120.9850, 'deep_pothole',     'high',   'Deep pothole that fills with stormwater when it rains.',          interval '2 days')
) as v (ticket, district, landmark, lat, lng, hazard, severity, description, age)
join public.districts d on d.name = v.district;

-- Move the sample reports through the workflow. Every status change below
-- goes through the history trigger, so status_updates fills up on its own.
update public.hazard_reports set status = 'under_inspection',
  status_note = 'Site secured with barricades. Drainage engineer en route to check the underground culvert.'
  where ticket_number = 'KW-8930';
update public.hazard_reports set status = 'under_inspection',
  status_note = 'Shoulder depression logged by patrol unit. Soil compaction check scheduled before patching.'
  where ticket_number = 'KW-8935';
update public.hazard_reports set status = 'under_inspection',
  status_note = 'Field inspection completed. Repair approved.'
  where ticket_number in ('KW-8921', 'KW-8924', 'KW-8910', 'KW-8902', 'KW-8898');

update public.hazard_reports set status = 'under_construction',
  status_note = 'Sub-base graded and hotmix compaction in progress. Lane divider cones deployed.'
  where ticket_number = 'KW-8921';
update public.hazard_reports set status = 'under_construction',
  status_note = 'Crack milling complete. Base gravel applied. Waiting for hotmix delivery.'
  where ticket_number = 'KW-8924';
update public.hazard_reports set status = 'under_construction',
  status_note = 'Repair crew dispatched.'
  where ticket_number in ('KW-8910', 'KW-8902', 'KW-8898');

update public.hazard_reports set status = 'repaired',
  status_note = 'About 40 meters resurfaced. Passed the density test and the final audit.'
  where ticket_number = 'KW-8910';
update public.hazard_reports set status = 'repaired',
  status_note = 'Road smoothed and pedestrian stripes repainted. Open to traffic.'
  where ticket_number = 'KW-8902';
update public.hazard_reports set status = 'repaired',
  status_note = 'Cavity filled with rapid-setting asphalt. Drainage test passed.'
  where ticket_number = 'KW-8898';
