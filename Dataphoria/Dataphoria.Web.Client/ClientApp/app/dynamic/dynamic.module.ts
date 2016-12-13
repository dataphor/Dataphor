import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

// parts module
import { PartsModule } from '../parts/parts.module';
import { InterfacesModule } from '../interfaces/interfaces.module';

// detail stuff
import { DynamicDetail } from './detail.view';
import { DynamicTypeBuilder } from './type.builder';
import { DynamicFormBuilder } from './form.builder';

@NgModule({
    imports: [PartsModule, InterfacesModule],
    declarations: [DynamicDetail],
    exports: [DynamicDetail],
})

export class DynamicModule {

    static forRoot() {
        return {
            ngModule: DynamicModule,
            providers: [ // singletons across the whole app
                DynamicFormBuilder,
                DynamicTypeBuilder
            ],
        };
    }
}