-- Link-Shot Phase 3: オンライン同期対戦用スキーマ
-- Supabaseダッシュボード > SQL Editor でこのファイルの内容をそのまま実行する。
-- 事前に Authentication > Providers > Anonymous Sign-Ins を有効化しておくこと。

create extension if not exists pgcrypto;

create table public.matches (
  id uuid primary key default gen_random_uuid(),
  room_code text unique not null,
  status text not null default 'waiting' check (status in ('waiting', 'active', 'finished')),
  player0_id uuid,
  player1_id uuid,
  first_attacker_player smallint,
  rng_seed integer,
  round smallint not null default 1,
  shot_index smallint not null default 0,
  phase text not null default 'CardSet',
  winner smallint,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create table public.match_actions (
  id uuid primary key default gen_random_uuid(),
  match_id uuid not null references public.matches(id) on delete cascade,
  sequence integer not null,
  action_type text not null,
  payload jsonb not null default '{}'::jsonb,
  created_at timestamptz not null default now(),
  unique (match_id, sequence)
);

create index match_actions_match_id_sequence_idx on public.match_actions (match_id, sequence);

alter table public.matches enable row level security;
alter table public.match_actions enable row level security;

-- matches: 参加者は常に読める。まだ埋まっていない(player1未参加)部屋はルームコードで誰でも見つけられる。
create policy "select matches" on public.matches
  for select
  using (
    status = 'waiting'
    or auth.uid() = player0_id
    or auth.uid() = player1_id
  );

-- matches: 部屋を作る本人がplayer0として作成する。
create policy "insert matches" on public.matches
  for insert
  with check (auth.uid() = player0_id);

-- matches: 参加者本人による更新、または空いている部屋への参加(player1として名乗り出る)を許可する。
-- with checkで、更新後は必ず自分が参加者になっていることを強制する。
create policy "update matches" on public.matches
  for update
  using (
    auth.uid() = player0_id
    or auth.uid() = player1_id
    or (status = 'waiting' and player1_id is null)
  )
  with check (
    auth.uid() = player0_id
    or auth.uid() = player1_id
  );

-- match_actions: その試合の参加者だけが読み書きできる。
create policy "select match_actions" on public.match_actions
  for select
  using (
    exists (
      select 1 from public.matches m
      where m.id = match_actions.match_id
        and (auth.uid() = m.player0_id or auth.uid() = m.player1_id)
    )
  );

create policy "insert match_actions" on public.match_actions
  for insert
  with check (
    exists (
      select 1 from public.matches m
      where m.id = match_actions.match_id
        and (auth.uid() = m.player0_id or auth.uid() = m.player1_id)
    )
  );
