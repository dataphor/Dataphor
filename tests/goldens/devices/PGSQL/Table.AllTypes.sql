EXECUTE create table "AllTypes"
(
	"ID" integer not null, 
	"Flag" integer not null, 
	"Amount" decimal(28, 8) not null, 
	"Note" character varying(8000) not null, 
	"Stamp" timestamp not null, 
	"Calendar" date not null, 
	"Clock" time not null, 
	"Identifier" uuid not null, 
	"Payload" bytea not null, 
	"Price" money not null
);
EXECUTE create unique index "UIDX_AllTypes_ID" on "AllTypes"("ID");
