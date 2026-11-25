EXECUTE create table "Ledger"
(
	"ID" integer not null, 
	"Note" varchar(30) not null, 
	"Amount" money not null
);
EXECUTE create unique clustered index "UIDX_Ledger_ID" on "Ledger"("ID");
