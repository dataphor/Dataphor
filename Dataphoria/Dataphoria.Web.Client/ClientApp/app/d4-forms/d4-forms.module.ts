import { forwardRef } from '@angular/core';
import { FormComponent, ColumnComponent, InterfaceComponent, NumericTextboxComponent, TextboxComponent } from './index';

export const DYNAMIC_DIRECTIVES = [
    forwardRef(() => FormComponent),
    forwardRef(() => ColumnComponent),
    forwardRef(() => InterfaceComponent),
    forwardRef(() => NumericTextboxComponent),
    forwardRef(() => TextboxComponent),
];

// module itself
import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { ReactiveFormsModule } from "@angular/forms";

@NgModule({
    imports: [
        CommonModule,
        ReactiveFormsModule
    ],
    declarations: [
        DYNAMIC_DIRECTIVES
    ],
    exports: [
        ReactiveFormsModule,
        CommonModule,
        DYNAMIC_DIRECTIVES
    ]
})
export class D4FormsModule {

    static forRoot() {
        return {
            ngModule: D4FormsModule,
            providers: [], // not used here, but if singleton needed
        };
    }
}