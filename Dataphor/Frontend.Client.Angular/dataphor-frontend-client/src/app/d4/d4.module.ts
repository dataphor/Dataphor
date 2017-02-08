import { forwardRef } from '@angular/core';
import {
    ColumnComponent,
    InterfaceComponent,
    MenuComponent,
    NumericTextboxComponent,
    ShowFormActionComponent,
    SourceComponent,
    SourceActionComponent,
    TextboxComponent
} from './frontend';

import {
    D4Component
} from './';

export const DYNAMIC_DIRECTIVES = [
    forwardRef(() => ColumnComponent),
    forwardRef(() => InterfaceComponent),
    forwardRef(() => MenuComponent),
    forwardRef(() => NumericTextboxComponent),
    forwardRef(() => ShowFormActionComponent),
    forwardRef(() => SourceComponent),
    forwardRef(() => SourceActionComponent),
    forwardRef(() => TextboxComponent),
    forwardRef(() => D4Component),
];

import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { ReactiveFormsModule } from "@angular/forms";

import { InterfaceService, NodeService } from './frontend';

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
export class D4Module {

    static forRoot() {
        return {
            ngModule: D4Module,
            providers: [
                InterfaceService,
                NodeService
            ], 
        };
    }
}