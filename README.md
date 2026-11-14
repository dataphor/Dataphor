# Dataphor

**Describe your data. Get the application.**

Dataphor is a platform for building business applications in which the schema *is* the program. You declare tables, keys, references, constraints and rules in D4, a truly relational language. Dataphor enforces what you declared, stores the data in the SQL systems you already run, and **derives the user interface from it**: browses, entry forms, lookups, master/detail navigation, search and ordering.

Applications built this way run to thousands of forms, and most of those forms were never written by anyone. They were inferred.

## One table definition in, an application out

This is a complete table definition from the video-rental sample in this repository:

```d4
create table Movie
{
	ID : Integer { default GetNextGenerator("Movie.ID") } tags { Frontend.Visible = "false" },
	Name : StrLth50 tags { Frontend.Width = "30" },
	Type : String tags { Frontend.Width = "6", Frontend.Browse.Visible = "false" },
	Rating : String tags { Frontend.Width = "7" },
	Year : Integer
	{
		constraint YearValid value >= 1891
			tags { DAE.SimpleMessage = "Year must be 1891 or later." }
	},
	key { ID },
	order { Name } tags { Frontend.IsDefault = "True" },
	reference Movies_MovieType { Type } references MovieType { ID },
	reference Movies_MovieRating { Rating } references MovieRating { ID }
};
```

Execute it, and the client can open `Derive('Movie', 'Browse')`. Without another line of code you have:

- a browse of movies, sorted by name, with incremental search on every order the table supports;
- add, edit, view and delete forms, laid out from the column types and widths;
- lookups for `Type` and `Rating`, because the references say where those values come from;
- navigation from a movie to everything that references it, and from those forms onward — the whole schema is reachable from wherever you start;
- the `YearValid` rule checked during entry, with your message, and enforced at the server no matter what client or script touches the table;
- generated keys, defaults and referential integrity, on whatever database holds the rows.

No form files were generated and nothing needs regenerating. Forms are derived on request from the live catalog, so when the schema changes, every form that depends on it has already changed.

## Why it works: the engine actually understands the schema

Deriving a form from a base table is the easy case. Dataphor derives them from *any expression*:

```d4
create view AvailableVideo
	VideoDetail
		left join (RentedVideo over { Video_ID }) by ID = Video_ID include rowexists
		where not rowexists
		over { ID, Name, Type, Rating, Year };
```

`Derive('AvailableVideo', 'Browse')` produces a working, searchable form here too, because the compiler does not treat a query as an opaque rowset. It infers keys, orders, references, constraints and metadata through every relational operator — join, restrict, project, extend, union — so the result of an expression is as fully described as a base table. The same inference is what makes views updatable: insert into a join and Dataphor knows which rows go where.

That is the idea the whole product is built around. A user interface, an integrity rule, a security check and a storage mapping are all consequences of the same logical model, so you state the model once and stop repeating it in four layers.

## When derived is not enough

Derived forms are the default, not a cage.

- **Tags** steer derivation from inside the schema: titles, visibility, grouping, widths, which columns appear on which kind of form.
- **Customization by difference.** Open any derived form in the visual designer, change it, and Dataphor stores only the delta. The customized form still inherits from the derived one, so later schema changes flow through underneath your changes.
- **Form inheritance.** Hand-built forms can inherit from other forms, derived or not, to any depth.
- **Actions and scripts** attach behaviour to forms: D4 executed at the server, or C# compiled at run time in the client.
- **Your own components.** Form nodes, D4 operators, scalar types and storage devices are all extension points with a small C# surface.

## What is in the box

**D4, a relational language.** Tables, views and expressions over a closed relational algebra; user-defined scalar types with multiple representations; keys, references, row, table and *transition* constraints; event handlers; operators and aggregate operators; exception handling; cursors when you want them. Designed in the tradition of *The Third Manifesto*, with a published account of where it complies and where it deliberately does not.

**A federated engine.** A *device* maps tables to a storage system. Queries are compiled once and split: each device is handed the largest piece it can execute natively, as SQL in its own dialect, and the engine does the rest — including joins *across* devices. SQL Server is the primary device, with PostgreSQL alongside it; an in-memory device and a simple file-backed device need nothing external. Integrity rules are enforced by Dataphor regardless of which system holds the data.

**Application transactions.** Long-running, optimistic units of work designed for data entry: a user can build up an order with its lines, see defaults and validation respond as they type, and commit or abandon the whole thing, without holding database locks while they think.

**The Frontend.** The derivation engine, a document store for forms, a Windows client that renders them, and a library of bound controls underneath.

**Dataphoria.** A development environment: schema browser, D4 editor with execution and plan analysis, visual form designer, and a debugger for server-side D4.

**The rest of a platform.** Libraries with versions, dependencies and upgrade scripts; users, roles and rights over every catalog object; an ADO.NET provider; over a thousand built-in operators; sample applications; and a body of D4 test scripts that doubles as a specification.

## Status

Dataphor has been in production since the early 2000s and open source since 2008. The last release line, 3.1, targets .NET Framework. **The project is now being modernized onto .NET 10**: one `dotnet` build, a real test suite, a new installer, a reorganized repository and documentation in Markdown.

Where that stands, honestly:

- The engine, compiler, server libraries and SQL Server device compile on .NET 10 essentially unchanged, and the engine boots and runs D4 there on a SQLite catalog. 62 of 76 existing D4 regression scripts pass untouched.
- The Windows client and Dataphoria compile on .NET 10 but have not yet been run there; that is the largest remaining piece of work.
- There is no one-command build yet. Until it lands, the old solution (`Dataphor.sln` at the repository root) needs Visual Studio and .NET Framework 4.5.2 targeting packs.

The full plan, with evidence and sequencing, is in [docs/modernization-plan.md](docs/modernization-plan.md). How the system fits together is in [docs/architecture.md](docs/architecture.md). Changes that existing applications will notice are tracked in [docs/migration-notes.md](docs/migration-notes.md).

## Learn more

The existing guides are thorough and still accurate about the language and the model:

- [Product tour](Documentation/UsersGuide/ProductTour.adoc) and the [movie-rental tutorial](Documentation/UsersGuide/Tutorial_MovieRental.adoc), which builds the application above step by step
- [Introduction to D4](Documentation/DevelopersGuide/D4Introduction.adoc) and [D4 compared with SQL](Documentation/DevelopersGuide/D4ComparisonWithSQL.adoc)
- [Form automation](Documentation/DevelopersGuide/FormAutomation.adoc) — how derivation decides what to build
- [Storage integration architecture](Documentation/DevelopersGuide/StorageIntegrationArchitecture.adoc) — how federation works
- [The full User's Guide](Documentation/UsersGuide/UsersGuide.adoc), [Developer's Guide](Documentation/DevelopersGuide/DevelopersGuide.adoc) and [Reference](Documentation/DataphorReference/DataphorReference.adoc)
- [Sample libraries](samples/) (`Sample.*`), from a one-form demonstration to a complete shipping application

## Contributing

Work is planned and tracked as tickets under [tickets/](tickets/); [AGENTS.md](AGENTS.md) is the orientation for anyone — human or AI agent — working in the code. The rule that matters most: existing applications are D4 libraries and form documents, and their behaviour is the specification.

## License

Dataphor was created at Alphora, beginning in 2000, and is released under a [BSD-style license](LICENSE).
