import { NgModule } from '@angular/core';
//import { BrowserModule } from '@angular/platform-browser';

//import { D4FormsModule } from '../d4-forms/d4-forms.module';

// detail stuff
//import { DynamicDetailComponent } from './detail.view';
import { DynamicTypeBuilder } from './type.builder';
import { DynamicFormBuilder } from './form.builder';

@NgModule({
    //imports: [D4FormsModule],
    //declarations: [DynamicDetailComponent],
    //exports: [DynamicDetailComponent],
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
