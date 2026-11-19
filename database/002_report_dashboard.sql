-- =====================================================================
-- Adds the homepage numbers view to an existing KalsadaWatch database.
-- Run this once in Supabase: SQL Editor -> New query -> paste -> Run.
-- It does not touch any data. (schema.sql already includes this view,
-- so a fresh setup only needs schema.sql.)
-- =====================================================================

drop view if exists public.report_dashboard;

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

grant select on public.report_dashboard to anon;
