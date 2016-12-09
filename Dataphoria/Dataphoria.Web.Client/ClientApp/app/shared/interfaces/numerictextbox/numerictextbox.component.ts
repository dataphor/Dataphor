import { Component, Input } from '@angular/core';

@Component({
    selector: 'd4-numerictextbox',
    template: require('./numerictextbox.component.html'),
    styles: [require('./numerictextbox.component.css')],
    providers: []
})
export class NumericTextboxComponent {
    
    @Input() title: string = '';
    @Input() name: string = '';
    @Input() nilifblank: string = '';

}
