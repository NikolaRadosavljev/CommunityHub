INSERT INTO users (id, username, password, name, surname, birthday, role) VALUES
(-1, 'marko', 'marko123', 'Marko', 'Marković', '2000-05-15', 'Manager'),
(-2, 'ana', 'ana123', 'Ana', 'Anić', '1998-08-22', 'Citizen'),
(-3, 'petar', 'petar123', 'Petar', 'Petrović', '2001-03-10', 'Tenant'),
(-4, 'jovana', 'jovana123', 'Jovana', 'Jovanović', '1999-11-30', 'Tenant'),
(-5, 'stefan', 'stefan123', 'Stefan', 'Stefanović', '2002-01-25', 'Cordinator'),
(-6, 'milica', 'milica123', 'Milica', 'Milić', '2000-07-08', 'Citizen'),
(-7, 'nikola', 'nikola123', 'Nikola', 'Nikolić', '1997-12-18', 'Cordinator'),
(-8, 'ivana', 'ivana123', 'Ivana', 'Ivanović', '2001-09-05', 'Citizen');

INSERT INTO posts (id, title, content, created_at, user_id) VALUES
(-1, 'Moj prvi post', 'Ovo je moj prvi post na platformi!', '2024-01-15 10:30:00', -1),
(-2, 'Zanimljiv dan', 'Danas sam imao vrlo zanimljiv dan na fakultetu.', '2024-01-17 09:15:00', -1),
(-3, 'Programiranje', 'Učim C# i WPF, jako je interesantno!', '2024-01-20 14:30:00', -1),
(-4, 'Dobrodošli', 'Drago mi je što sam ovde.', '2024-01-16 14:20:00', -2),
(-5, 'Vikend planovi', 'Šta planirate za vikend?', '2024-01-19 16:00:00', -2),
(-6, 'Nova knjiga', 'Počela sam da čitam novu knjigu o programiranju.', '2024-01-22 11:00:00', -2),
(-7, 'Nova tema', 'Želim da podelim nešto interesantno o bazama podataka.', '2024-01-18 11:45:00', -3),
(-8, 'SQL je moćan', 'SQL je neverovatno moćan jezik za rad sa podacima.', '2024-01-21 10:00:00', -3),
(-9, 'Postgres', 'Postgres je odličan sistem za upravljanje bazama.', '2024-01-23 15:30:00', -3),
(-10, 'Učenje programiranja', 'Programiranje je fascinantno!', '2024-01-20 08:30:00', -4),
(-11, 'Moj projekat', 'Radim na zanimljivom projektu.', '2024-01-22 13:15:00', -4),
(-12, 'Pozdrav svima', 'Zdravo svima iz zajednice!', '2024-01-21 12:00:00', -5),
(-13, 'Sport', 'Volim da trčim ujutru pre posla.', '2024-01-23 07:00:00', -5),
(-14, 'Muzika', 'Slušam jazz muziku dok programiram.', '2024-01-24 18:30:00', -5),
(-15, 'Moje iskustvo', 'Delim svoje iskustvo sa bazama podataka.', '2024-01-22 15:30:00', -6),
(-16, 'Kafa', 'Najbolja kafa je ujutru!', '2024-01-24 08:00:00', -6),
(-17, 'Fudbal', 'Volim da gledam fudbal vikendom.', '2024-01-21 16:00:00', -7),
(-18, 'Putovanje', 'Planiram putovanje na more.', '2024-01-24 12:00:00', -7),
(-19, 'Film', 'Pogledala sam odličan film sinoć.', '2024-01-22 20:00:00', -8),
(-20, 'Trening', 'Redovno treniram u teretani.', '2024-01-25 06:30:00', -8);

INSERT INTO buildings (id, address, settlement, city, country, floor_number, manager_id) VALUES
(-1, 'Kralja Petra 12', 'Centar', 'Novi Sad', 'Srbija', 2, -1);

INSERT INTO flats (id, building_id, floor_number, flat_number) VALUES
(-1, -1, 1, 1),
(-2, -1, 1, 2),
(-3, -1, 2, 3),
(-4, -1, 2, 4);

INSERT INTO building_requests (id, user_id, building_id, flat_number, created_at, status, rejection_explanation) VALUES
(-1, -2, -1, 2, '2026-04-01 10:00:00', 'Pending', NULL),
(-2, -3, -1, 3, '2026-03-20 12:00:00', 'Denied', 'Stan je već zauzet'),
(-3, -4, -1, 4, '2026-03-25 09:30:00', 'Accepted', NULL);

INSERT INTO building_memberships (id, building_id, user_id, floor_number, flat_number, joined_at) VALUES
(-1, -1, -4, 2, 4, '2026-03-25 09:30:00');

INSERT INTO countries (id, name) VALUES
(-1, 'Srbija');

INSERT INTO cities (id, name, postalCode, country_id) VALUES
(-1, 'Novi Sad', '21000', -1),
(-2, 'Beograd', '11000', -1);

INSERT INTO neighborhoods (id, coordinator_id, name, description, budget, city_id, images) VALUES
(-1, -5, 'Liman', 'Mirno stambeno naselje pored Dunava.', 50000.00, -1, '{}'),
(-2, -7, 'Telep', 'Popularno naselje na jugu Novog Sada.', 30000.00, -1, '{}'),
(-3, -5, 'Zvezdara', 'Zeleno naselje u Beogradu.', 45000.00, -2, '{}');

INSERT INTO streets (id, name, start_number, end_number, neighborhood_id) VALUES
(-1, 'Bulevar oslobođenja', 1, 100, -1),
(-2, 'Partizanska', 1, 50, -1),
(-3, 'Gogoljeva', 1, 30, -2),
(-4, 'Cara Dušana', 1, 80, -2),
(-5, 'Kralja Petra', 1, 60, -3);

INSERT INTO citizens (user_id, address) VALUES
(-2, 'Bulevar oslobođenja 15'),
(-6, 'Partizanska 8'),
(-8, 'Gogoljeva 25');

-- Common rooms for building -1
INSERT INTO common_spaces (id, building_id, name, description, floor_number, is_available_for_multiple_days) VALUES
(-1, -1, 'Sala za žurke', 'Prostorija za proslave i okupljanja stanara.', 1, false),
(-2, -1, 'Teretana', 'Opremljena teretana dostupna svim stanarima.', 2, false),
(-3, -1, 'Soba za sastanke', 'Mirna prostorija za poslovne sastanke i učenje.', 1, true);

-- Rental requests for common rooms
INSERT INTO rental_requests (id, common_room_id, tenant_id, start_date, end_date, status, created_at, suggested_start_date, suggested_end_date) VALUES
(-1, -1, -4, '2026-05-10', '2026-05-10', 'Approved', '2026-05-01 10:00:00', NULL, NULL),
(-2, -1, -4, '2026-05-20', '2026-05-20', 'Pending', '2026-05-01 11:00:00', NULL, NULL),
(-3, -2, -4, '2026-05-12', '2026-05-12', 'Approved', '2026-05-01 12:00:00', NULL, NULL),
(-4, -3, -4, '2026-05-05', '2026-05-07', 'Approved', '2026-05-01 13:00:00', NULL, NULL),
(-5, -3, -4, '2026-05-15', '2026-05-17', 'Pending', '2026-05-01 14:00:00', NULL, NULL);

INSERT INTO events (id, neighborhood_id, organizer_id, name, description, date, start_time, duration_minutes, min_volunteers, status) VALUES
(-1, -1, -2, 'Čišćenje parka', 'Zajednička akcija čišćenja centralnog parka u Limanu.', '2026-06-15', '09:00', 120, 3, 'Preparation'),
(-2, -1, -6, 'Sadnja drveća', 'Sadnja novih stabala duž bulevara.', '2026-06-20', '10:00', 180, 2, 'Preparation'),
(-3, -2, -8, 'Čišćenje ulica', 'Akcija čišćenja ulica u Telepu.', '2026-05-01', '08:00', 90, 2, 'Finished');

INSERT INTO event_items (id, event_id, name) VALUES
(-1, -1, 'Kese za smeće'),
(-2, -1, 'Rukavice'),
(-3, -1, 'Metle'),
(-4, -2, 'Sadnice'),
(-5, -2, 'Lopate'),
(-6, -3, 'Kese za smeće'),
(-7, -3, 'Rukavice');

INSERT INTO event_volunteers (id, event_id, citizen_id, attended) VALUES
(-1, -3, -2, true),
(-2, -3, -8, false);

INSERT INTO event_volunteer_items (volunteer_id, item_id) VALUES
(-1, -6),
(-2, -7);