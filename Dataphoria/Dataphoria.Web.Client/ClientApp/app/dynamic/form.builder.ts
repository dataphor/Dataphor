import { Injectable } from "@angular/core";

@Injectable()
export class DynamicFormBuilder {

    public prepareForm(entity: any, d4Interface: any) {


        //let properties = Object.keys(entity);
        //let template = "<form >";
        //let editorName = useTextarea
        //    ? "text-editor"
        //    : "string-editor";

        //properties.forEach((propertyName) => {
        //    template += `
        //  <${editorName}
        //      [propertyName]="'${propertyName}'"
        //      [entity]="entity"
        //  ></${editorName}>`;
        //});

        //return template + "</form>";

        return d4Interface;

    }

}