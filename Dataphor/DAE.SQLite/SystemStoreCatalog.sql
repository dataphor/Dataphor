/*
	System Store Structures
	Dataphor Server

	These are the structures required to support a persistent catalog store in the DAE.
	On first connection to the configured catalog device, if the DAE_ServerInfo table is 
	not found, this script will be run to setup the initial catalog. These structures will
	then be populated with the core catalog objects. Core catalog objects include only those
	objects that are explicitly created on startup of the server and include the system devices
	such as catalog and temp, and the core system data types.

	This script is written to be used with SQLite 3.
*/

/*
	Table: DAEServerInfo
	
	Contains one row and stores server-wide settings, as well as the Version of the DAE that 
	created this store.
*/

create table DAEServerInfo
(
	ID nchar(2) not null default 'ID', --check (ID = 'ID'),
	Name nvarchar(200) not null,
	Version nvarchar(20) not null,
	MaxConcurrentProcesses int not null,
	ProcessWaitTimeout int not null,
	ProcessTerminationTimeout int not null,
	PlanCacheSize int not null,
	constraint PK_DAEServerInfo primary key (ID)
)
go

/*
	Table: DAEUsers
	
	Contains one row for each user configured in the DAE.
*/

create table DAEUsers
(
	ID nvarchar(200) not null,
	Name nvarchar(200) not null,
	Data nvarchar(200) null,
	constraint PK_DAEUsers primary key (ID)
	--reference Users_Groups { Group_Name } references Groups { Name }
)
go

/*
	Table: DAELoadedLibraries
	
	Stores the set of libraries loaded in the DAE.
*/

create table DAELoadedLibraries
(
	Library_Name nvarchar(200) not null,
	constraint PK_DAELoadedLibraries primary key (Library_Name)
	--reference LoadedLibraries_Libraries { Library_Name } references Libraries { Name }
)
go

/*
	Table: DAELibraryDirectories
	
	Stores the directory for a library that is attached from an explicit directory,
	rather than the primary library directory for the server.
*/

create table DAELibraryDirectories
(
	Library_Name nvarchar(200) not null,
	Directory nvarchar(200) not null,
	constraint PK_DAELibraryDirectories primary key (Library_Name)
	--reference LibraryDirectories_Libraries { Library_Name } references Libraries { Name }
)
go

/*
	Table: DAELibraryVersions
	
	Stores the currently loaded version of each library.
*/

create table DAELibraryVersions
(
	Library_Name nvarchar(200) not null,
	VersionNumber nvarchar(40) not null,
	constraint PK_DAELibraryVersions primary key (Library_Name)
	--reference LibraryVersions_Libraries { Library_Name } references Libraries { Name }
)
go

/*
	Table: DAELibraryOwners
	
	Stores the owner for each library.
*/

create table DAELibraryOwners
(
	Library_Name nvarchar(200) not null,
	Owner_User_ID nvarchar(200) not null,
	constraint PK_DAELibraryOwners primary key (Library_Name)
	--reference LibraryOwners_Libraries { Library_Name } references Libraries { Name }
	--reference LibraryOwners_Users { Owner_User_ID } references Users { ID }
)
go

/*
	Table: DAEObjects
	
	Stores the header information for each object in the catalog.
*/

create table DAEObjects
(
	ID int not null,
	Name nvarchar(200) not null,
	Library_Name nvarchar(200) not null,
	DisplayName nvarchar(200) not null,
	Description nvarchar(200) not null,
	Type nvarchar(80) not null,
	IsSystem tinyint not null default 0, -- check (IsSystem in (0, 1)),
	IsRemotable tinyint not null default 0, -- check (IsRemotable in (0, 1)),
	IsGenerated tinyint not null default 0, -- check (IsGenerated in (0, 1)),
	IsATObject tinyint not null default 0, -- check (IsATObject in (0, 1)),
	IsSessionObject tinyint not null default 0, -- check (IsSessionObject in (0, 1)),
	IsPersistent tinyint not null default 0, -- check (IsPersistent in (0, 1)),
	Catalog_Object_ID int null,
	Parent_Object_ID int null,
	Generator_Object_ID int null,
	ServerData ntext,
	constraint PK_DAEObjects primary key (ID)
	--reference Objects_Objects { Parent_Object_ID } references Objects { ID },
	--reference Objects_Libraries { Library_Name } references Libraries { Name }
)
go

create index IDX_DAEObjects_Catalog_Object_ID on DAEObjects (Catalog_Object_ID)
go

create index IDX_DAEObjects_Parent_Object_ID on DAEObjects (Parent_Object_ID)
go

create index IDX_DAEObjects_Generator_Object_ID on DAEObjects (Generator_Object_ID)
go

/*
	Table: DAEObjectDependencies
	
	Stores the dependency information for each object in the catalog.
*/

create table DAEObjectDependencies
(
	Object_ID int not null,
	Dependency_Object_ID int not null,
	constraint PK_DAEObjectDependencies primary key (Object_ID, Dependency_Object_ID)
	--reference ObjectDependencies_Objects { Object_ID } references Objects { ID },
	--reference ObjectDependencies_Dependency_Objects { Dependency_Object_ID } references Objects { ID }
)
go

create index IDX_DAEObjectDependencies_Dependency_Object_ID on DAEObjectDependencies (Dependency_Object_ID)
go

/*
	Table: DAECatalogObjects
	
	Stores the serialization information for each catalog object in the catalog.
*/

create table DAECatalogObjects
(
	ID int not null,
	Name nvarchar(200) not null,
	Library_Name nvarchar(200) not null,
	Owner_User_ID nvarchar(200) not null,
	constraint PK_DAECatalogObjects primary key (ID)
	--reference CatalogObjects_Objects { ID } references Objects { ID },
	--reference CatalogObjects_Users { Owner_User_ID } references Users { ID }
)
go

--reference Objects_CatalogObjects Objects { Catalog_Object_ID } references CatalogObjects { ID };
create index IDX_DAECatalogObjects_Owner_User_ID on DAECatalogObjects (Owner_User_ID)
go

create index IDX_DAECatalogObjects_Library_Name on DAECatalogObjects (Library_Name)
go

create unique index UIDX_DAECatalogObjects_Name on DAECatalogObjects (Name)
go

/*
	Table: DAECatalogObjectNames

	This table is effectively an index for an EndsWith operator optimized to match only on qualifier boundaries.
*/

create table DAECatalogObjectNames
(
	Depth int not null,
	Name nvarchar(200) not null, --This is the name of the object specified by ID with name dequalified Depth times
	ID int not null,
	constraint PK_DAECatalogObjectNames primary key (Depth, Name, ID)
	--reference CatalogObjectNames_CatalogObjects { ID } references CatalogObjects { ID }
)
go

create index IDX_DAECatalogObjectNames_ID on DAECatalogObjectNames (ID)
go

/*
	Table: DAEBaseCatalogObjects
	
	Stores the set of base catalog objects. See the catalog startup documentation in Server.cs for more information.
*/

create table DAEBaseCatalogObjects
(
	ID int not null,
	constraint PK_DAEBaseCatalogObjects primary key (ID)
	--reference BaseCatalogObjects_CatalogObjects { ID } references CatalogObjects { ID }
)
go

/*
	Table: DAEScalarTypes
	
	Stores information specific to scalar types. Each scalar type will in DAEObjects will have a row in this table.
*/

create table DAEScalarTypes
(
	ID int not null,
	Unique_Sort_ID int not null,
	Sort_ID int not null,
	constraint PK_DAEScalarTypes primary key (ID)
	--reference ScalarTypes_CatalogObjects { ID } references CatalogObjects { ID }
	--reference ScalarTypes_Sorts { Sort_ID } references Sorts { ID }
	--reference ScalarTypes_Unique_Sorts { Unique_Sort_ID } references Sorts { ID }
)
go

/*
	Table: DAEOperatorNames
	
	Stores the set of operator names for all operators in the catalog. Each operator name
	specified in this table will have N corresponding rows in the DAEOperators table
	detailing which operators have this operator name.
*/

create table DAEOperatorNames
(
	Name nvarchar(200) not null,
	constraint PK_DAEOperatorNames primary key (Name)
)
go

/*
	Table: DAEOperatorNameNames

	This table serves the same purpose as CatalogObjectNames but for OperatorNames (not the names of operators), 
	which actually exist in a distinct namespace from catalog objects. i.e. it is legal to have an operator with 
	an OperatorName of ID, and a scalar type named ID.
*/

create table DAEOperatorNameNames
(
	Depth int not null,
	Name nvarchar(200) not null, --This is the operator name (not just the name) of the operator specified by ID with name dequalified Depth times
	OperatorName nvarchar(200) not null,
	constraint PK_DAEOperatorNameNames primary key (Depth, Name, OperatorName)
	--reference OperatorNameNames { OperatorName } references OperatorNames { Name }
)
go

create index IDX_DAEOperatorNameNames_OperatorName on DAEOperatorNameNames (OperatorName)
go

/*
	Table: DAEOperators
	
	Stores the OperatorName for each operator in the Catalog.
*/

create table DAEOperators
(
	ID int not null,
	OperatorName nvarchar(200) not null,
	Signature ntext not null,
	Locator ntext not null,
	Line int not null,
	LinePos int not null,
	constraint PK_DAEOperators primary key (ID)
	--reference Operators_OperatorNames { OperatorName } references OperatorNames { Name },
	--reference Operators_CatalogObjects { ID } references CatalogObjects { ID },
)
go

create index IDX_DAEOperators_OperatorName on DAEOperators (OperatorName)
go

/*
	Table: DAEEventHandlers
	
	Stores the linking information for each event handler
*/

create table DAEEventHandlers
(
	ID int not null, -- Object ID of the event handler
	Operator_ID int not null, -- Object ID of the operator handling the event
	Source_Object_ID int not null, -- Object ID of the object raising the event
	constraint PK_DAEEventHandlers primary key (ID)
	--reference EventHandlers_CatalogObjects { ID } references CatalogObjects { ID },
	--reference EventHandlers_Operators { Operator_ID } references Operators {ID },
	--reference EventHandlers_Objects { Source_Object_ID } references Objects { ID },
)
go

create index IDX_DAEEventHandlers_Operator_ID on DAEEventHandlers (Operator_ID)
go

create index IDX_DAEEventHandlers_Source_Object_ID on DAEEventHandlers (Source_Object_ID)
go

/*
	Table: DAEApplicationTransactionTableMaps
	
	Stores the set of translated A/T table variables
*/

create table DAEApplicationTransactionTableMaps
(
	Source_TableVar_ID int not null,
	Translated_TableVar_ID int not null,
	Deleted_TableVar_ID int not null,
	constraint PK_DAEApplicationTransactionTableMaps primary key (Source_TableVar_ID)
)
go

/*
	Table: DAEApplicationTransactionOperatorNameMaps
	
	Stores the set of translated A/T operator names
*/

create table DAEApplicationTransactionOperatorNameMaps
(
	Source_OperatorName nvarchar(200) not null,
	Translated_OperatorName nvarchar(200) not null,
	constraint PK_DAEApplicationTransactionOperatorNameMaps primary key (Source_OperatorName)
)
go

/*
	Table: DAEApplicationTransactionOperatorMaps
	
	Stores the set of translated A/T operators
*/

create table DAEApplicationTransactionOperatorMaps
(
	Source_Operator_ID int not null,
	Translated_Operator_ID int not null,
	constraint PK_DAEApplicationTransactionOperatorMaps primary key (Source_Operator_ID)
)
go

create index IDX_DAEApplicationTransactionOperatorMaps_Translated_Operator_ID on DAEApplicationTransactionOperatorMaps (Translated_Operator_ID)
go

/*
	Table: DAEUserRoles
	
	Specifies the set of roles that each user is a member of.
*/

create table DAEUserRoles
(
	User_ID nvarchar(200) not null,
	Role_ID int not null,
	constraint PK_DAEUserRoles primary key (User_ID, Role_ID)
	--reference UserRoles_Users { User_ID } references Users { ID },
	--reference UserRoles_Roles { Role_ID } references Roles { ID }	
)
go

create index IDX_DAEUserRoles_Role_ID on DAEUserRoles (Role_ID)
go

/*
	Table: DAERights
	
	Stores the set of rights available in the DAE. If the right was
	created for a Catalog object, Catalog_Object_ID will be the ID
	of that object. Otherwise, Catalog_Object_ID will be -1.
*/

create table DAERights
(
	Name nvarchar(200) not null,
	Owner_User_ID nvarchar(200) not null,
	Catalog_Object_ID int not null,
	constraint PK_DAERights primary key (Name)
	--reference Rights_Users { Owner_User_ID } references Users { ID }
	--reference Rights_CatalogObjects { Catalog_Object_ID } references CatalogObjects { ID }
)
go

create index IDX_DAERights_Owner_User_ID on DAERights (Owner_User_ID)
go

create index IDX_DAERights_Catalog_Object_ID on DAERights (Catalog_Object_ID)
go

/*
	Table: DAERoleRightAssignments
	
	Stores the set of right assignments for each role.
*/

create table DAERoleRightAssignments
(
	Role_ID int not null,
	Right_Name nvarchar(200) not null,
	IsGranted tinyint not null default 0, -- check (IsGranted in (0, 1)),
	constraint PK_DAERoleRightAssignments primary key (Role_ID, Right_Name)
	--reference RoleRightAssignments_Roles { Role_ID } references Roles { ID },
	--reference RoleRightAssignmetns_Right { Right_Name } references Rights { Name }
)
go

create index IDX_DAERoleRightAssignments_Right_Name on DAERoleRightAssignments (Right_Name)
go

/*
	Table: DAEUserRightAssignments
	
	Stores the set of right assignments for each user.
*/

create table DAEUserRightAssignments
(
	User_ID nvarchar(200) not null,
	Right_Name nvarchar(200) not null,
	IsGranted tinyint not null default 0, -- check (IsGranted in (0, 1)),
	constraint PK_DAEUserRightAssignments primary key (User_ID, Right_Name)
	--reference UserRightAssignments_Users { User_ID } references Users { ID },
	--reference UserRightAssignments_Rights { Right_Name } references Rights { Name }
)
go

create index IDX_DAEUserRightAssignments_Right_Name on DAEUserRightAssignments (Right_Name)
go

/*
	Table: DAEDevices
	
	Stores the set of catalog objects that are devices.
*/

create table DAEDevices
(
	ID int not null,
	ReconciliationMaster nvarchar(20) not null,
	ReconciliationMode nvarchar(80) not null,
	constraint PK_DAEDevices primary key (ID)
	--reference Devices_CatalogObjects (ID) references CatalogObjects (ID)
)
go

/*
	Table: DAEDeviceUsers
	
	Stores the set of device user maps for each user.
*/

create table DAEDeviceUsers
(
	User_ID nvarchar(200) not null,
	Device_ID int not null,
	UserID nvarchar(200) not null,
	Data nvarchar(200) not null,
	ConnectionParameters nvarchar(500) not null,
	constraint PK_DAEDeviceUsers primary key (User_ID, Device_ID)
	--reference DeviceUsers_Users { User_ID } references Users { ID },
	--reference DeviceUsers_Devices { Device_ID } references Devices { ID },
)
go

create index IDX_DAEDeviceUsers_Device_ID on DAEDeviceUsers (Device_ID)
go

/*
	Table: DAEDeviceObjects
	
	Stores the set of device objects for each device.
*/

create table DAEDeviceObjects
(
	ID int not null,
	Device_ID int not null,
	Mapped_Object_ID int not null,
	constraint PK_DAEDeviceObjects primary key (ID, Device_ID, Mapped_Object_ID)
	--reference DeviceObjects_CatalogObjects { ID } references CatalogObjects { ID },
	--reference DeviceObjects_Devices { Device_ID } references Devices { ID },
	--reference DeviceObjects_Mapped_CatalogObjects { Mapped_Object_ID } references CatalogObjects { ID },
)
go

create index IDX_DAEDeviceObjects_Device_ID_Mapped_Object_ID on DAEDeviceObjects (Device_ID, Mapped_Object_ID)
go

/*
	Table: DAEClasses
	
	Stores the list of classes registered in all loaded libraries.
*/

create table DAEClasses
(
	Name nvarchar(200) not null,
	Library_Name nvarchar(200) not null,
	constraint PK_DAEClasses primary key (Name)
	--reference Classes_LoadedLibraries { Library_Name } references LoadedLibraries { Library_Name },
)
go

