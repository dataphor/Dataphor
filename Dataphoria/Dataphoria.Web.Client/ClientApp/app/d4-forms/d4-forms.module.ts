import { forwardRef } from '@angular/core';
import {
    ColumnComponent,
    InterfaceComponent,
    MenuComponent,
    NumericTextboxComponent,
    ShowFormActionComponent,
    SourceComponent,
    SourceActionComponent,
    TextboxComponent,
    FormComponent
} from './index';

export const DYNAMIC_DIRECTIVES = [
    forwardRef(() => ColumnComponent),
    forwardRef(() => InterfaceComponent),
    forwardRef(() => MenuComponent),
    forwardRef(() => NumericTextboxComponent),
    forwardRef(() => ShowFormActionComponent),
    forwardRef(() => SourceComponent),
    forwardRef(() => SourceActionComponent),
    forwardRef(() => TextboxComponent),
    forwardRef(() => FormComponent),
];

import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { ReactiveFormsModule } from "@angular/forms";
import { InterfaceService } from './interface/index';
import { NodeService } from './nodes/index';

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
            providers: [
                InterfaceService,
                NodeService
            ], 
        };
    }
}