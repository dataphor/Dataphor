EXECUTE create table "Ledger"
(
	"ID" integer not null, 
	"Note" character varying(30) not null, 
	"Amount" money not null
);
EXECUTE create unique index "UIDX_Ledger_ID" on "Ledger"("ID");
