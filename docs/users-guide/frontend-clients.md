# Frontend Clients

## Summary

Dataphor Frontend Clients are responsible for two distinct functions. First, a Frontend Client manages the connectivity and data access for the presentation layer, and second, it processes Dataphor Form Documents, rendering them as user interfaces appropriate for the platform on which they are running.

The connectivity and data access mechanisms are managed using the Data Access Layer exposed by the Dataphor Server. This involves managing connection state with the server, as well as data presentation and state management such as buffering, and editing data.

The process of rendering user interfaces is specific to each client, but all the clients request Dataphor Form Documents from the Dataphor Server using the Document API exposed by the Frontend Server.

Because the Dataphor Frontend Client architecture is designed to be consumed from multiple clients, the same application schema will be available on any general-purpose Dataphor Frontend Client.

## Windows Client

The Windows Client is a Windows executable program that communicates with the Dataphor Server to request user interface descriptions, and to retrieve and manipulate the data in the database. The Windows Client makes use of the Dataphor Data Access Components to provide access to the Dataphor Server. These components take full advantage of the application-targeted features exposed by the Dataphor Server.

## Clients That Are No Longer Shipped

Release 3.x also shipped two browser-hosted Frontend Clients: a Web Client, which was an ASP.NET web application rendering HTML pages, and a Silverlight client. Release 4.0 ships neither of them, and the Windows Client described above is the only client included in the product. See the [migration notes](../migration-notes.md) for what this means for an existing application.
