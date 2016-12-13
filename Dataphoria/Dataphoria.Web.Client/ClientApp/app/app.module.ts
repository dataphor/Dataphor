import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { FormsModule} from '@angular/forms';
import { UniversalModule } from 'angular2-universal';
import { AceEditorDirective } from 'ng2-ace-editor';
import { COMPILER_PROVIDERS } from '@angular/compiler';

import { AppComponent } from './components/app/app.component'
import { HomeComponent } from './components/home/home.component';
import { TestComponent } from './components/test/test.component';

import { DynamicModule } from './dynamic/dynamic.module';
import { InterfacesModule } from './interfaces/interfaces.module';
import { SharedModule } from './shared/shared.module';

@NgModule({
    bootstrap: [ AppComponent ],
    declarations: [
        AppComponent,
        HomeComponent,
        TestComponent,
        AceEditorDirective
    ],
    providers: [
        COMPILER_PROVIDERS
    ],
    imports: [
        UniversalModule, // Must be first import. This automatically imports BrowserModule, HttpModule, and JsonpModule too.
        FormsModule,
        RouterModule.forRoot([
            { path: '', redirectTo: 'home', pathMatch: 'full' },
            { path: 'home', component: HomeComponent },
            { path: 'test', component: TestComponent },
            { path: '**', redirectTo: 'home' }
        ]),
        DynamicModule.forRoot(),
        InterfacesModule.forRoot(),
        SharedModule.forRoot()
    ]
})
export class AppModule {
}
