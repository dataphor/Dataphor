//import { APIService, IResponse, ResponseSingle, ResponseSet } from '../../shared/index';

import { Component } from '@angular/core';

@Component({
    selector: 'test',
    template: require('./test.component.html'),
    styles: [require('./test.component.css')],
    providers: [APIService]
})
export class TestComponent {
    

}
