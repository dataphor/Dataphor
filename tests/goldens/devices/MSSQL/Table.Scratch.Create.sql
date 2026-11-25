EXECUTE create table "Scratch"
(
	"ID" integer not null
);
EXECUTE create unique clustered index "UIDX_Scratch_ID" on "Scratch"("ID");
