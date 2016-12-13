import { Injectable } from "@angular/core";

@Injectable()
export class DynamicFormBuilder {

    public prepareForm() {

        return `<d4-interface>
    <d4-column name="RootEditColumn">
        <d4-column name="Element1">

        </d4-column>
    </d4-column>
</d4-interface>`;

    }

}