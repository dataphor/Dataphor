EXECUTE create table "AllTypes"
(
	"ID" integer not null, 
	"Flag" integer not null, 
	"Amount" decimal(28, 8) not null, 
	"Note" varchar(8000) not null, 
	"Stamp" datetime not null, 
	"Calendar" datetime not null, 
	"Clock" datetime not null, 
	"Identifier" uniqueidentifier not null, 
	"Payload" image not null, 
	"Price" money not null
);
EXECUTE create unique clustered index "UIDX_AllTypes_ID" on "AllTypes"("ID");
