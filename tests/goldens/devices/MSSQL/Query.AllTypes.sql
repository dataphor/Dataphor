select 
	"T1"."ID" as "ID", "T1"."Flag" as "Flag", "T1"."Amount" as "Amount", "T1"."Note" as "Note", "T1"."Stamp" as "Stamp", "T1"."Calendar" as "Calendar", "T1"."Clock" as "Clock", "T1"."Identifier" as "Identifier", "T1"."Payload" as "Payload", "T1"."Price" as "Price"
	from "AllTypes" as "T1" 
order by "ID" asc
option (fast 1);
