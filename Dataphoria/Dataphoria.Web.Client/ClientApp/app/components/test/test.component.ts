//import { APIService, IResponse, ResponseSingle, ResponseSet } from '../../shared/index';

import { Component, OnInit } from '@angular/core';
import { TestForm } from './form';

@Component({
    selector: 'test',
    template: require('./test.component.html'),
    styles: [require('./test.component.css')],
    providers: []
})
export class TestComponent implements OnInit {

    ngOnInit() {

    }


}
