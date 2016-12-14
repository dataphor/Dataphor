import { Component, Input } from '@angular/core';

@Component({
    selector: 'd4-textbox',
    template: require('./textbox.component.html'),
    styles: [require('./textbox.component.css')],
    providers: []
})
export class TextboxComponent {
    
    @Input() title: string = '';
    @Input() name: string = '';

    @Input() public propertyName: string;
    @Input() public entity: any;

    

    OnSubmit() {
        
    }

}
