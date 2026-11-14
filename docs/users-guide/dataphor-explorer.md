# Dataphor Explorer

1.  [Libraries](dataphoria-libraries.md)
2.  [Applications](dataphoria-applications.md)
3.  [Security](dataphoria-security.md)

## Summary

The Dataphor Explorer provides a hierarchical representation of the entire catalog of the Dataphor Server. Each level of the tree exposes different types of functionality. This functionality is accessible by right-clicking on a node to show the context menu for that node of the tree. It is important to note that all the functionality exposed by Dataphoria is available as operators and statements of the D4 language. Dataphoria simply provides a visual interface to enhance the productivity of a developer working with the platform. The following sections consider each type of node in the tree.

## Server

The server node is the root node of the Dataphor Explorer, and exposes the functionality available for the entire server.

Choosing Upgrade Libraries traverses the catalog in library-dependency order and ensures that any outstanding upgrades have been applied. Selecting this menu option is equivalent to invoking the operator UpgradeLibraries with no parameters.
