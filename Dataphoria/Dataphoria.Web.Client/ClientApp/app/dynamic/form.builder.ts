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
        let d4Form = "<d4-form>\r\n";
        d4Form += d4Interface;
        d4Form += "\r\n";
        d4Form += "</d4-form>";
        return d4Form;

    }

}