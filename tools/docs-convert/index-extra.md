## Project documents

- [Architecture](architecture.md): how the parts of Dataphor fit together, what talks to what, and where things live on disk. One page per subsystem, under `architecture/`:
  - [Assemblies and projects](architecture/assemblies.md): which assembly holds what, and which project builds it.
  - [Sessions, processes, plans](architecture/sessions-processes-plans.md): how a connection becomes a session, a process, a plan and a cursor.
  - [Catalog and the catalog store](architecture/catalog-and-store.md): the schema objects the server keeps, and the SQL database it persists them in.
  - [Libraries and class loading](architecture/libraries-and-class-loading.md): what a library is, and how a D4 class name reaches a .NET type.
  - [Devices](architecture/devices.md): what translates a query into a back end's own language.
  - [Frontend: from schema to screen](architecture/frontend.md): how a form is derived from the catalog and built on the client.
  - [Hosting, protocol, discovery](architecture/hosting-protocol-discovery.md): what loads an instance and what it serves, on which addresses.
  - [On disk](architecture/on-disk.md): where a deployment's files live.
  - [What is alive](architecture/what-is-alive.md): which projects are in use, which are parked, and what the port removed.
  - [Third-party code in the tree](architecture/third-party-code.md): the third-party libraries and binaries the tree carries.
- [Modernization plan](modernization-plan.md): the plan for moving Dataphor to .NET 10, with its workstreams, decisions and sequencing.
- [Migration notes](migration-notes.md): the changes that an application, a deployment script or a custom extension will notice.
- [Test coverage report](coverage-report.md): what the test suite reaches in the engine and what it does not, measured on one full run with the coverage collector switched on.
- [Performance baseline](perf-baseline.md): how fast the engine is, recorded by the test suite and by the benchmark program so that a large slowdown is visible, and the procedure for the one-off comparison with 3.1.
- [Contributing](contributing/README.md): how to build, test and document the project - what you need installed, which solution to open, where the build puts things, and the conventions the documentation under `docs/` follows.
