# Starting Dataphor Applications

## Summary

The first step in running a Dataphor Application is to start the desired Frontend Client. This can be accomplished in a variety of ways, depending on the client to be started, and the way it is installed. A typical Windows Client installation will add the Windows Client icon to the Start \| Programs menu in the Microsoft Windows environment.

For more information on what client should be used, and how it should be started, consult your Dataphor Application Administrator.

## Connecting to a Dataphor Server

Running a Dataphor Application begins with connecting to a Dataphor Server. The first step is to select an *alias* to use to connect. Each alias contains settings for connecting to a Dataphor Server such as the name of the server. The following screen capture shows the Dataphor Server Login form that displays when the Windows Client starts:

![Remote logging into Dataphor Server](../images/dataphor-server-login-remote.png)

Once the appropriate alias has been selected and login information supplied, click the Login button to connect to the Dataphor Server.

For more information on configuring aliases, see the [Aliases](aliases.md) section of the Dataphoria chapter in the Using Dataphor part of the Dataphor User’s Guide.

## Selecting an Dataphor Application

The Dataphor Server is capable of hosting multiple applications, so the next step is to select the desired application. The following screen capture shows the Choose Application form:

![Choosing an Application in Windows Client](../images/windows-client-choose-application.png)

Select the desired application by clicking it, or using the up and down arrow keys to highlight the application in the list. Clicking OK or pressing Enter will launch the application.

## Bypassing Login and Application Selection

The Login form, the Choose Application form, or both can be bypassed at startup by configuring a shortcut appropriately. To do this, first create a shortcut to the Windows Client application. One way to do this is by right-clicking on the `WindowsClient.exe` file and selecting Send To \| Desktop (create shortcut).

Right-click on the newly created shortcut and select Properties. The following screen capture shows the resulting form:

![Windows Client Properties - Shortcut](../images/windows-client-shortcut-properties.png)

In the Target: text box, add the following to the end of the file name:

- Automatically select the alias to use: `-alias <alias name>`, where `<alias name>` is the name of the alias to be used. For example: `-alias Remote`
- Automatically select the application to run: `-application "<application id>"` where `<application id>` is the application identifier for the application. For example: `-application "Shipping"`

The application id is a unique identifier for the application on the server. To obtain this identifier for a specific application, consult your Dataphor Application Administrator.
