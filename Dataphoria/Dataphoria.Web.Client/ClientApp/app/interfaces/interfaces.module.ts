import { forwardRef } from '@angular/core';
import { ColumnComponent, InterfaceComponent, NumericTextboxComponent, TextboxComponent } from './index';

export const DYNAMIC_DIRECTIVES = [
    forwardRef(() => ColumnComponent),
    forwardRef(() => InterfaceComponent),
    forwardRef(() => NumericTextboxComponent),
    forwardRef(() => TextboxComponent),
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
        DYNAMIC_DIRECTIVES
    ],
    exports: [
        DYNAMIC_DIRECTIVES,
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