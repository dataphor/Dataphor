import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule }   from '@angular/forms';

import { ColumnComponent, InterfaceComponent, NumericTextboxComponent, TextboxComponent, APIService } from './index';

/**
 * Do not specify providers for modules that might be imported by a lazy loaded module.
 */

@NgModule({
    imports: [CommonModule, RouterModule, FormsModule],
    declarations: [
        ColumnComponent, InterfaceComponent, NumericTextboxComponent, TextboxComponent
    ],
    exports: [
        ColumnComponent, InterfaceComponent, NumericTextboxComponent, TextboxComponent
    ],
    providers: [APIService]
})
export class SharedModule {
    static forRoot(): ModuleWithProviders {
        return {
            ngModule: SharedModule
        };
    }
}