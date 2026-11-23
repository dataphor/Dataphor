# Dataphor

## Summary

Welcome to [Dataphor’s](http://www.dataphor.com) documentation suite.

We’ve divided our documentation into three main categories:

**[User’s Guide](users-guide/README.md)**  
Introduces Dataphor and provides usage guidance.

This Guide provides the following:

- An overview of the motivation behind Dataphor
- A Dataphor platform introduction, covering the anatomy of a Dataphor application and providing tutorials
- A section on using applications built with Dataphor, including Dataphor Server administration

**[Developer’s Guide](developers-guide/README.md)**  
Discusses the declarative approach employed in Dataphor as a solution to many of the technical problems associated with producing software.

It is primarily intended to be read by software engineers; however, much of the material is pertinent for application and database administrators as well.

**[Dataphor Reference](reference/README.md)**  
Dataphor Reference documents the Dataphor product, including the Call-Level Interface (CLI), Dataphor Frontend, Tags, documentation for all the libraries that ship with the Dataphor product, as well as class references for using Dataphor’s Data Access Layers (DAL) from a Microsoft .NET language such as C#.

## Contents

1.  [Introduction](README.md)

### User’s Guide

1.  [User’s Guide](users-guide/README.md)
    1.  [Product Tour](users-guide/product-tour.md)
        1.  [Architectural Overview](users-guide/architectural-overview.md)
        2.  [Dataphor Server](users-guide/dataphor-server.md)
        3.  [Dataphor Frontend](users-guide/dataphor-frontend.md)
        4.  [Dataphoria Summary](users-guide/dataphoria-summary.md)
        5.  [Frontend Clients](users-guide/frontend-clients.md)
    2.  [Dataphoria](users-guide/dataphoria.md)
        1.  [Aliases](users-guide/aliases.md)
            1.  [Process Defaults](users-guide/process-defaults.md)
            2.  [Client-side Buffering Controls](users-guide/client-side-buffering-controls.md)
        2.  [Dataphor Explorer](users-guide/dataphor-explorer.md)
            1.  [Libraries](users-guide/dataphoria-libraries.md)
            2.  [Applications](users-guide/dataphoria-applications.md)
            3.  [Security](users-guide/dataphoria-security.md)
        3.  [Documents and Designers](users-guide/documents-and-designers.md)
    3.  [Dataphor Service](users-guide/dataphor-service.md)
    4.  [Security](users-guide/security.md)
    5.  [Using Dataphor Applications](users-guide/using-dataphor-applications.md)
        1.  [Starting a Dataphor Application](users-guide/starting-dataphor-applications.md)
        2.  [Using Dataphor Forms](users-guide/using-dataphor-forms.md)
        3.  [Using Dataphor Controls (UI Components)](users-guide/using-dataphor-controls.md)
    6.  [Tutorials](users-guide/tutorials.md)
        1.  [Movie Rental](users-guide/tutorial-movie-rental.md)
        2.  [Book Publishing (with Pubs sample database)](users-guide/tutorial-book-publishing.md)
        3.  [Frontend Components](users-guide/tutorial-sample-components.md)

### Developer’s Guide

1.  [Developer’s Guide](developers-guide/README.md)
    1.  [Introduction](developers-guide/introduction.md)
        1.  [Application Development Considerations](developers-guide/application-development-considerations.md)
        2.  [Database Management Concepts](developers-guide/database-management-concepts.md)
        3.  [Application Development Concepts](developers-guide/application-development-concepts.md)
        4.  [Dataphor Product Anatomy](developers-guide/dataphor-product-anatomy.md)
    2.  [Logical Application Design](developers-guide/logical-application-design.md)
        1.  [Preliminaries](developers-guide/preliminaries.md)
        2.  [Implementing Data Types](developers-guide/implementing-data-types.md)
        3.  [Representing Data with Tables and Views](developers-guide/representing-data.md)
        4.  [Enforcing Business Rules Declaratively](developers-guide/enforcing-business-rules.md)
        5.  [Modeling Process Logic](developers-guide/modeling-process-logic.md)
        6.  [Enforcing Stimulus-Response Rules](developers-guide/enforcing-stimulus-response-rules.md)
        7.  [Enforcing Security](developers-guide/enforcing-security.md)
        8.  [Working With Libraries](developers-guide/working-with-libraries.md)
        9.  [Database Design Techniques](developers-guide/database-design-techniques.md)
    3.  [Presentation Layer](developers-guide/presentation-layer.md)
        1.  [Overview](developers-guide/presentation-overview.md)
        2.  [Putting it all Together](developers-guide/putting-it-all-together.md)
        3.  [Form Automation](developers-guide/form-automation.md)
        4.  [The Space Between the Data](developers-guide/space-between-data.md)
    4.  [Physical Realization](developers-guide/physical-realization.md)
        1.  [Storage Integration Architecture](developers-guide/storage-integration-architecture.md)
        2.  [Devices](developers-guide/devices.md)
        3.  [Scalar Type Mapping](developers-guide/scalar-type-mapping.md)
        4.  [Operator Mapping](developers-guide/operator-mapping.md)
    5.  [D4 Language Guide](developers-guide/d4-language-guide.md)
        1.  [Introduction](developers-guide/d4-introduction.md)
        2.  [Scripts and Execution](developers-guide/d4-scripts-and-execution.md)
        3.  [Lexical Elements](developers-guide/d4-lexical-elements.md)
        4.  [Language Elements](developers-guide/d4-language-elements.md)
        5.  [Catalog Elements](developers-guide/d4-catalog.md)
        6.  [Data Manipulation](developers-guide/d4-data-manipulation.md)
        7.  [Table Operators](developers-guide/d4-table-operators.md)
        8.  [Imperative Statements](developers-guide/d4-imperative-statements.md)
        9.  [Comparison with SQL](developers-guide/d4-comparison-with-sql.md)

### Dataphor Reference

1.  [Dataphor Reference](reference/README.md)
    1.  [D4 Language Grammar](reference/d4-grammar.md)
    2.  [Frontend Components](reference/frontend-nodes/README.md)
    3.  [Call-level Interface (CLI)](reference/call-level-interface.md)
    4.  [Data Access Components (DAC)](reference/data-access-components.md)
    5.  [Error Message Prefix Codes](reference/error-codes.md)
    6.  [Tags](reference/tags.md)
    7.  [Libraries](reference/libraries/README.md)

### Misc

1.  [Glossary](glossary.md)
2.  [Bibliography](bibliography.md)
3.  Authoring Documentation

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
