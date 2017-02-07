import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { FormsModule} from '@angular/forms';
import { HttpModule } from '@angular/http';

import { AceEditorDirective } from 'ng2-ace-editor';

import { AppComponent } from './app.component';
import { HomeComponent } from './components/home/home.component';
import { TestComponent } from './components/test/test.component';

import { DynamicModule } from './dynamic/dynamic.module';
import { D4FormsModule } from './d4-forms/d4-forms.module';
import { SharedModule } from './shared/shared.module';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    TestComponent,
    AceEditorDirective
  ],
  imports: [
    BrowserModule,
    FormsModule,
    HttpModule,
    RouterModule.forRoot([
            { path: '', redirectTo: 'home', pathMatch: 'full' },
            { path: 'home', component: HomeComponent },
            { path: 'test', component: TestComponent },
            { path: '**', redirectTo: 'home' }
        ]),
    DynamicModule,
    // D4FormsModule.forRoot(),
    SharedModule.forRoot()
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
