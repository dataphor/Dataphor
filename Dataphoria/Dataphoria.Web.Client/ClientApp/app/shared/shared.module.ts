import { NgModule, ModuleWithProviders } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

//import { FooterComponent } from './footer/index';
//import { NavbarComponent } from './navbar/index';
//import { StyledMapComponent } from './styled-map/index';

import { NavMenuComponent } from './navmenu/index';

//import { WaypointDirective, WaypointPositionFactory } from './waypoint/index';

import { APIService } from './api/index';

/**
 * Do not specify providers for modules that might be imported by a lazy loaded module.
 */

@NgModule({
    imports: [CommonModule, RouterModule],
    declarations: [NavMenuComponent],
    //declarations: [FooterComponent, NavbarComponent, StyledMapComponent, WaypointDirective],
    exports: [
        NavMenuComponent
    //    FooterComponent,
    //    NavbarComponent,
    //    CommonModule,
    //    RouterModule,
    //    StyledMapComponent,
    //    WaypointDirective
    ],
    //providers: [WaypointPositionFactory]
    providers: [APIService]
})
export class SharedModule {
    static forRoot(): ModuleWithProviders {
        return {
            ngModule: SharedModule
        };
    }
}