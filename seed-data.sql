-- Seed data for AuHub Auctions Database
-- Runs on first DB initialization (before migrations)
-- Tables are created by EF Core migrations, so we use DO blocks for safe insertion

-- Lot 1: Active auction - Золотая монета
INSERT INTO "Lots" ("Id", "Title", "Description", "StartingPrice", "CurrentPrice", "StartTime", "EndTime", "SellerId", "Status", "CreatedAt", "UpdatedAt", "WinnerId")
SELECT
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'Золотая монета 10 рублей 1899 года',
    'Редкая золотая монета Российской Империи в отличном состоянии. Сохранность XF (Extremely Fine). Тираж ограничен.',
    50000, 50000,
    NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
    NOW() AT TIME ZONE 'UTC' + INTERVAL '3 days',
    '11111111-1111-1111-1111-111111111111',
    'Active',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
    NULL
WHERE NOT EXISTS (SELECT 1 FROM "Lots" WHERE "Id" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890');

-- Lot 2: Active auction - Картина
INSERT INTO "Lots" ("Id", "Title", "Description", "StartingPrice", "CurrentPrice", "StartTime", "EndTime", "SellerId", "Status", "CreatedAt", "UpdatedAt", "WinnerId")
SELECT
    'b2c3d4e5-f6a7-8901-bcde-f12345678901',
    'Картина «Закат над Волгой», масло, холст',
    'Оригинальная работа современного художника. Размер 60x80 см. Оформлена в багет.',
    25000, 28500,
    NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days',
    NOW() AT TIME ZONE 'UTC' + INTERVAL '1 day',
    '11111111-1111-1111-1111-111111111111',
    'Active',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '5 hours',
    NULL
WHERE NOT EXISTS (SELECT 1 FROM "Lots" WHERE "Id" = 'b2c3d4e5-f6a7-8901-bcde-f12345678901');

-- Lot 3: Active auction - Значки
INSERT INTO "Lots" ("Id", "Title", "Description", "StartingPrice", "CurrentPrice", "StartTime", "EndTime", "SellerId", "Status", "CreatedAt", "UpdatedAt", "WinnerId")
SELECT
    'c3d4e5f6-a7b8-9012-cdef-123456789012',
    'Коллекция из 50 советских значков',
    'Политические и памятные значки 1960-1980-х годов. Все в хорошем состоянии.',
    3000, 4200,
    NOW() AT TIME ZONE 'UTC' - INTERVAL '3 days',
    NOW() AT TIME ZONE 'UTC' + INTERVAL '2 days',
    '11111111-1111-1111-1111-111111111111',
    'Active',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '3 days',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
    NULL
WHERE NOT EXISTS (SELECT 1 FROM "Lots" WHERE "Id" = 'c3d4e5f6-a7b8-9012-cdef-123456789012');

-- Lot 4: Active auction - Письменный прибор
INSERT INTO "Lots" ("Id", "Title", "Description", "StartingPrice", "CurrentPrice", "StartTime", "EndTime", "SellerId", "Status", "CreatedAt", "UpdatedAt", "WinnerId")
SELECT
    'd4e5f6a7-b8c9-0123-defa-234567890123',
    'Антикварный письменный прибор, бронза',
    'Чернильница с подсвечником, Франция, конец XIX века. Патина, гравировка.',
    15000, 15000,
    NOW() AT TIME ZONE 'UTC' - INTERVAL '12 hours',
    NOW() AT TIME ZONE 'UTC' + INTERVAL '5 days',
    '11111111-1111-1111-1111-111111111111',
    'Active',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '12 hours',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '12 hours',
    NULL
WHERE NOT EXISTS (SELECT 1 FROM "Lots" WHERE "Id" = 'd4e5f6a7-b8c9-0123-defa-234567890123');

-- Lot 5: Draft - Серебряные приборы
INSERT INTO "Lots" ("Id", "Title", "Description", "StartingPrice", "CurrentPrice", "StartTime", "EndTime", "SellerId", "Status", "CreatedAt", "UpdatedAt", "WinnerId")
SELECT
    'e5f6a7b8-c9d0-1234-efab-345678901234',
    'Серебряный набор столовых приборов (12 предметов)',
    'Серебро 925 пробы, СССР, 1950-е годы. В оригинальном футляре.',
    35000, 35000,
    NOW() AT TIME ZONE 'UTC' + INTERVAL '1 day',
    NOW() AT TIME ZONE 'UTC' + INTERVAL '8 days',
    '11111111-1111-1111-1111-111111111111',
    'Draft',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '6 hours',
    NOW() AT TIME ZONE 'UTC' - INTERVAL '6 hours',
    NULL
WHERE NOT EXISTS (SELECT 1 FROM "Lots" WHERE "Id" = 'e5f6a7b8-c9d0-1234-efab-345678901234');

-- Bids for Lot 1
INSERT INTO "Bids" ("Id", "LotId", "BidderId", "Amount", "PlacedAt")
SELECT '10000000-0000-0000-0000-000000000001', 'a1b2c3d4-e5f6-7890-abcd-ef1234567890', '22222222-2222-2222-2222-222222222222', 52000, NOW() AT TIME ZONE 'UTC' - INTERVAL '20 hours'
WHERE NOT EXISTS (SELECT 1 FROM "Bids" WHERE "Id" = '10000000-0000-0000-0000-000000000001');

INSERT INTO "Bids" ("Id", "LotId", "BidderId", "Amount", "PlacedAt")
SELECT '10000000-0000-0000-0000-000000000002', 'a1b2c3d4-e5f6-7890-abcd-ef1234567890', '22222222-2222-2222-2222-222222222222', 55000, NOW() AT TIME ZONE 'UTC' - INTERVAL '15 hours'
WHERE NOT EXISTS (SELECT 1 FROM "Bids" WHERE "Id" = '10000000-0000-0000-0000-000000000002');

-- Bids for Lot 2
INSERT INTO "Bids" ("Id", "LotId", "BidderId", "Amount", "PlacedAt")
SELECT '10000000-0000-0000-0000-000000000003', 'b2c3d4e5-f6a7-8901-bcde-f12345678901', '22222222-2222-2222-2222-222222222222', 26000, NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day'
WHERE NOT EXISTS (SELECT 1 FROM "Bids" WHERE "Id" = '10000000-0000-0000-0000-000000000003');

INSERT INTO "Bids" ("Id", "LotId", "BidderId", "Amount", "PlacedAt")
SELECT '10000000-0000-0000-0000-000000000004', 'b2c3d4e5-f6a7-8901-bcde-f12345678901', '22222222-2222-2222-2222-222222222222', 28500, NOW() AT TIME ZONE 'UTC' - INTERVAL '5 hours'
WHERE NOT EXISTS (SELECT 1 FROM "Bids" WHERE "Id" = '10000000-0000-0000-0000-000000000004');

-- Bids for Lot 3
INSERT INTO "Bids" ("Id", "LotId", "BidderId", "Amount", "PlacedAt")
SELECT '10000000-0000-0000-0000-000000000005', 'c3d4e5f6-a7b8-9012-cdef-123456789012', '22222222-2222-2222-2222-222222222222', 3500, NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days'
WHERE NOT EXISTS (SELECT 1 FROM "Bids" WHERE "Id" = '10000000-0000-0000-0000-000000000005');

INSERT INTO "Bids" ("Id", "LotId", "BidderId", "Amount", "PlacedAt")
SELECT '10000000-0000-0000-0000-000000000006', 'c3d4e5f6-a7b8-9012-cdef-123456789012', '22222222-2222-2222-2222-222222222222', 4200, NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day'
WHERE NOT EXISTS (SELECT 1 FROM "Bids" WHERE "Id" = '10000000-0000-0000-0000-000000000006');
