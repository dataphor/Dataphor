export const TestForm = {
    'model': [
        {
            'id': 'MainColumnMain',
            'value': [
                {
                    'id': 'ID',
                    'value': 'SomeID' 
                },
                {
                    'id': 'Name',
                    'value': 'John Doe'
                }
            ]
        }
    ],
    'interface': {
        'value': `
        <d4-interface>
            <d4-column name="RootEditColumn">
                <d4-column name="Element1">
                    <d4-numerictextbox name="MainColumnMain.ID" nilifblank="True"></d4-numerictextbox>
                    <d4-textbox name="MainColumnMain.Name" title="Customized Name"></d4-textbox>
                </d4-column>
            </d4-column>
        </d4-interface>
        `
    }
};