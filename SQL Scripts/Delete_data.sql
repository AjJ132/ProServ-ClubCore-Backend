/*TRUNCATE TABLE "AspNetRoleClaims", "AspNetRoles", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserRoles", "AspNetUserTokens", "AspNetUsers", "CalendarEvents", "Teams", "Users" CASCADE;

*/
TRUNCATE TABLE "DirectConversations" CASCADE;
TRUNCATE TABLE "GroupConversation" CASCADE;

-- DO
-- $$
-- DECLARE
--     r RECORD;
-- BEGIN
--     FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = current_schema()) LOOP
--         EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
--     END LOOP;
-- END
-- $$;
