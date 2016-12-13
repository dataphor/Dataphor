// make all interfaces as one DYNAMIC_DIRECTIVES 
import { forwardRef } from '@angular/core';
//import { StringEditor } from './string';
//import { TextEditor } from './text';
import { ColumnComponent, InterfaceComponent, NumericTextboxComponent, TextboxComponent } from './index';

export const DYNAMIC_DIRECTIVES = [
    //forwardRef(() => ColumnComponent),
    //forwardRef(() => InterfaceComponent),
    //forwardRef(() => NumericTextboxComponent),
    //forwardRef(() => TextboxComponent),
    //forwardRef(() => TextEditor)
];

// module itself
import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";

@NgModule({
    imports: [
        CommonModule,
        FormsModule
    ],
    declarations: [
        //DYNAMIC_DIRECTIVES
        ColumnComponent, InterfaceComponent, NumericTextboxComponent, TextboxComponent
    ],
    exports: [
        //DYNAMIC_DIRECTIVES,
        ColumnComponent, InterfaceComponent, NumericTextboxComponent, TextboxComponent,
        CommonModule,
        FormsModule
    ]
})
export class InterfacesModule {

    static forRoot() {
        return {
            ngModule: InterfacesModule,
            providers: [], // not used here, but if singleton needed
        };
    }
}