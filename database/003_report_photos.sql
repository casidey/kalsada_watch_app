-- =====================================================================
-- Adds report photos to an existing KalsadaWatch database.
-- Run this once in Supabase: SQL Editor -> New query -> paste -> Run.
-- It does not change any existing data. (schema.sql already includes
-- all of this, so a fresh setup only needs schema.sql.)
-- =====================================================================

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

grant select, insert on public.report_photos to anon;  -- photos are never edited or deleted by the app
alter table public.report_photos enable row level security;
create policy "read_all"         on public.report_photos for select to anon using (true);
create policy "insert_evidence"  on public.report_photos for insert to anon
  with check (photo_type = 'evidence');  -- citizens add evidence; progress/completion photos come from crews later

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
