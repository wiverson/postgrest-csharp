﻿
-- Create the Replication publication 
CREATE PUBLICATION supabase_realtime FOR ALL TABLES;

-- Create a second schema
CREATE SCHEMA personal;

-- USERS
CREATE TYPE public.user_status AS ENUM ('ONLINE', 'OFFLINE');
CREATE TABLE public.users (
  username text primary key,
  inserted_at timestamp without time zone DEFAULT timezone('utc'::text, now()) NOT NULL,
  updated_at timestamp without time zone DEFAULT timezone('utc'::text, now()) NOT NULL,
  favorite_numbers int[] DEFAULT null,
  data jsonb DEFAULT null,
  age_range int4range DEFAULT null,
  status user_status DEFAULT 'ONLINE'::public.user_status,
  catchphrase tsvector DEFAULT null
);
ALTER TABLE public.users REPLICA IDENTITY FULL; -- Send "previous data" to supabase 
COMMENT ON COLUMN public.users.data IS 'For unstructured data and prototyping.';

-- CHANNELS
CREATE TABLE public.channels (
  id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  inserted_at timestamp without time zone DEFAULT timezone('utc'::text, now()) NOT NULL,
  updated_at timestamp without time zone DEFAULT timezone('utc'::text, now()) NOT NULL,
  data jsonb DEFAULT null,
  slug text
);
ALTER TABLE public.users REPLICA IDENTITY FULL; -- Send "previous data" to supabase
COMMENT ON COLUMN public.channels.data IS 'For unstructured data and prototyping.';

-- MESSAGES
CREATE TABLE public.messages (
  id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  inserted_at timestamp without time zone DEFAULT timezone('utc'::text, now()) NOT NULL,
  updated_at timestamp without time zone DEFAULT timezone('utc'::text, now()) NOT NULL,
  data jsonb DEFAULT null,
  message text,
  username text REFERENCES users NOT NULL,
  channel_id bigint REFERENCES channels NOT NULL
);
ALTER TABLE public.messages REPLICA IDENTITY FULL; -- Send "previous data" to supabase
COMMENT ON COLUMN public.messages.data IS 'For unstructured data and prototyping.';

create table "public"."kitchen_sink" (
  "id" serial primary key,
  "string_value" varchar(255) null,
  "unique_value" varchar(255) UNIQUE,
  "int_value" INT null,
  "float_value" FLOAT null,
  "double_value" DOUBLE PRECISION null,
  "datetime_value" timestamp null,
  "datetime_value_1" timestamp null,
  "datetime_pos_infinite_value" timestamp null,
  "datetime_neg_infinite_value" timestamp null,
  "list_of_strings" TEXT [ ] null,
  "list_of_datetimes" DATE [ ] null,
  "list_of_ints" INT [ ] null,
  "list_of_floats" FLOAT [ ] null,
  "int_range" INT4RANGE null
);

CREATE TABLE public.movie (
  id int GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  created_at timestamp without time zone NOT NULL DEFAULT now(),
  name character varying(255) NULL
);

CREATE TABLE public.person (
  id int GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  created_at timestamp without time zone NOT NULL DEFAULT now(),
  first_name character varying(255) NULL,
  last_name character varying(255) NULL
);

CREATE TABLE public.profile (
  profile_id int PRIMARY KEY references person(id),
  email character varying(255) null,
  created_at timestamp without time zone NOT NULL DEFAULT now()
);

CREATE TABLE public.movie_person (
  id int generated by default as identity,
  movie_id int references movie(id),
  person_id int references person(id),
  primary key(id, movie_id, person_id)
);

insert into "public"."movie" ("created_at", "id", "name") values ('2022-08-20 00:29:45.400188', 1, 'Top Gun: Maverick');
insert into "public"."movie" ("created_at", "id", "name") values ('2022-08-20 00:29:45.400188', 2, 'Mad Max: Fury Road');

insert into "public"."person" ("created_at", "first_name", "id", "last_name") values ('2022-08-20 00:30:02.120528', 'Tom', 1, 'Cruise');
insert into "public"."person" ("created_at", "first_name", "id", "last_name") values ('2022-08-20 00:30:02.120528', 'Tom', 2, 'Holland');
insert into "public"."person" ("created_at", "first_name", "id", "last_name") values ('2022-08-20 00:30:33.72443', 'Bob', 3, 'Saggett');

insert into "public"."profile" ("created_at", "email", "profile_id") values ('2022-08-20 00:30:33.72443', 'tom.cruise@supabase.io', 1);
insert into "public"."profile" ("created_at", "email", "profile_id") values ('2022-08-20 00:30:33.72443', 'tom.holland@supabase.io', 2);
insert into "public"."profile" ("created_at", "email", "profile_id") values ('2022-08-20 00:30:33.72443', 'bob.saggett@supabase.io', 3);

insert into "public"."movie_person" ("id", "movie_id", "person_id") values (1, 1, 1);
insert into "public"."movie_person" ("id", "movie_id", "person_id") values (2, 2, 2);
insert into "public"."movie_person" ("id", "movie_id", "person_id") values (3, 1, 3);


-- STORED FUNCTION
CREATE FUNCTION public.get_status(name_param text)
RETURNS user_status AS $$
  SELECT status from users WHERE username=name_param;
$$ LANGUAGE SQL IMMUTABLE;

-- SECOND SCHEMA USERS
CREATE TYPE personal.user_status AS ENUM ('ONLINE', 'OFFLINE');
CREATE TABLE personal.users(
  username text primary key,
  inserted_at timestamp without time zone DEFAULT timezone('utc'::text, now()) NOT NULL,
  updated_at timestamp without time zone DEFAULT timezone('utc'::text, now()) NOT NULL,
  data jsonb DEFAULT null,
  age_range int4range DEFAULT null,
  status user_status DEFAULT 'ONLINE'::public.user_status
);

-- SECOND SCHEMA STORED FUNCTION
CREATE FUNCTION personal.get_status(name_param text)
RETURNS user_status AS $$
  SELECT status from users WHERE username=name_param;
$$ LANGUAGE SQL IMMUTABLE;
