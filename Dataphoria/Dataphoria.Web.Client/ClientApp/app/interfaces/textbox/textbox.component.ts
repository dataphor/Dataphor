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

    model = {
        name: this.name
    };

    OnSubmit() {
        
    }

}
