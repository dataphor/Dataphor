import { Component, Input } from '@angular/core';

@Component({
    selector: 'd4-interface',
    template: require('./interface.component.html'),
    styles: [require('./interface.component.css')],
    providers: []
})
export class InterfaceComponent {

    @Input() text: string = '';
    @Input() mainsource: string = '';

}