export const TestForm = {
    'model': {
        'MainColumnMain': {
            'ID': '001',
            'Name': 'John Doe',
            'Phone': '7777777777',
            'Address': 'Scruff McGruff',
            'City': 'Chicago',
            'State': 'Illinois',
            'Zip': '60652'
        }
    },
    'interface': {
        'value': `
            <d4-interface text="Edit Users" mainsource="Main">
              <d4-source name="Main" expression="(&#xD;&amp;#xA; System.Users&amp;#xD;&amp;#xA;  rename Main&amp;#xD;&amp;#xA;)&amp;#xD;&amp;#xA; browse by { Main.ID asc include nil }&amp;#xD;&amp;#xA;capabilities { navigable, backwardsnavigable, bookmarkable, searchable, updateable }&amp;#xD;&amp;#xA;isolation browse"></d4-source>
              <d4-showformaction name="Main.ID.System.Sessions_Users" text="Sessions..." document=".Frontend.Derive('System.Sessions', 'List', 'Main.ID', 'Main.User_ID', true)" sourcelinktype="Detail" sourcelink.source="Main" sourcelink.masterkeynames="Main.ID" sourcelink.detailkeynames="Main.User_ID" sourcelinkrefresh="False"></d4-showformaction>
              <d4-showformaction name="Main.ID.System.CatalogObjects_Users" text="CatalogObjects..." document=".Frontend.Derive('System.CatalogObjects', 'List', 'Main.ID', 'Main.Owner_User_ID', true)" sourcelinktype="Detail" sourcelink.source="Main" sourcelink.masterkeynames="Main.ID" sourcelink.detailkeynames="Main.Owner_User_ID" sourcelinkrefresh="False"></d4-showformaction>
              <d4-showformaction name="Main.ID.System.Rights_Users" text="Rights..." document=".Frontend.Derive('System.Rights', 'List', 'Main.ID', 'Main.Owner_User_ID', true)" sourcelinktype="Detail" sourcelink.source="Main" sourcelink.masterkeynames="Main.ID" sourcelink.detailkeynames="Main.Owner_User_ID" sourcelinkrefresh="False"></d4-showformaction>
              <d4-showformaction name="Main.ID.System.UserRightAssignments_Users" text="UserRightAssignments..." document=".Frontend.Derive('System.UserRightAssignments', 'List', 'Main.ID', 'Main.User_ID', true)" sourcelinktype="Detail" sourcelink.source="Main" sourcelink.masterkeynames="Main.ID" sourcelink.detailkeynames="Main.User_ID" sourcelinkrefresh="False"></d4-showformaction>
              <d4-showformaction name="Main.ID.System.DeviceUsers_Users" text="DeviceUsers..." document=".Frontend.Derive('System.DeviceUsers', 'List', 'Main.ID', 'Main.User_ID', true)" sourcelinktype="Detail" sourcelink.source="Main" sourcelink.masterkeynames="Main.ID" sourcelink.detailkeynames="Main.User_ID" sourcelinkrefresh="False"></d4-showformaction>
              <d4-showformaction name="Main.ID.System.ServerLinkUsers_Users" text="ServerLinkUsers..." document=".Frontend.Derive('System.ServerLinkUsers', 'List', 'Main.ID', 'Main.User_ID', true)" sourcelinktype="Detail" sourcelink.source="Main" sourcelink.masterkeynames="Main.ID" sourcelink.detailkeynames="Main.User_ID" sourcelinkrefresh="False"></d4-showformaction>
              <d4-showformaction name="Main.ID.System.UserRoles_Users" text="UserRoles..." document=".Frontend.Derive('System.UserRoles', 'List', 'Main.ID', 'Main.User_ID', true)" sourcelinktype="Detail" sourcelink.source="Main" sourcelink.masterkeynames="Main.ID" sourcelink.detailkeynames="Main.User_ID" sourcelinkrefresh="False"></d4-showformaction>
              <d4-menu name="DetailsMenuItem" text="De&amp;tails">
                <d4-menu name="Main.ID.System.Sessions_UsersDetailsMenuItem" action="Main.ID.System.Sessions_Users"></d4-menu>
                <d4-menu name="Main.ID.System.CatalogObjects_UsersDetailsMenuItem" action="Main.ID.System.CatalogObjects_Users"></d4-menu>
                <d4-menu name="Main.ID.System.Rights_UsersDetailsMenuItem" action="Main.ID.System.Rights_Users"></d4-menu>
                <d4-menu name="Main.ID.System.UserRightAssignments_UsersDetailsMenuItem" action="Main.ID.System.UserRightAssignments_Users"></d4-menu>
                <d4-menu name="Main.ID.System.DeviceUsers_UsersDetailsMenuItem" action="Main.ID.System.DeviceUsers_Users"></d4-menu>
                <d4-menu name="Main.ID.System.ServerLinkUsers_UsersDetailsMenuItem" action="Main.ID.System.ServerLinkUsers_Users"></d4-menu>
                <d4-menu name="Main.ID.System.UserRoles_UsersDetailsMenuItem" action="Main.ID.System.UserRoles_Users"></d4-menu>
              </d4-menu>
              <d4-sourceaction name="Validate" source="Main" text="&amp;Validate" action="Validate"></d4-sourceaction>
              <d4-column name="RootEditColumn">
                <d4-column name="Element1">
                  <d4-textbox name="MainColumnMain.ID" title="User ID" maxlength="255" maxwidth="15" nilifblank="False" width="10" source="Main" columnname="Main.ID"></d4-textbox>
                  <d4-textbox name="MainColumnMain.Name" title="Name" nilifblank="False" source="Main" columnname="Main.Name"></d4-textbox>
                </d4-column>
              </d4-column>
            </d4-interface>
        `
    }
};