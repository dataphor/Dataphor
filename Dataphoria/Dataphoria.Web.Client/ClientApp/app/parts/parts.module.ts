// make all parts as one DYNAMIC_DIRECTIVES 
import { forwardRef } from '@angular/core';
import { StringEditor } from './string';
import { TextEditor } from './text';

export const DYNAMIC_DIRECTIVES = [
    forwardRef(() => StringEditor),
    forwardRef(() => TextEditor)
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
export class PartsModule {

    static forRoot() {
        return {
            ngModule: PartsModule,
            providers: [], // not used here, but if singleton needed
        };
    }
}