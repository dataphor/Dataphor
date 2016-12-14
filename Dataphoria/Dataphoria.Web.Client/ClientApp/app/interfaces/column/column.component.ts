import { Component, Input } from '@angular/core';

@Component({
    selector: 'd4-column',
    template: require('./column.component.html'),
    styles: [require('./column.component.css')],
    providers: []
})
export class ColumnComponent {
    
    @Input() title: string;
    @Input() name: string;

    @Input() public propertyName: string;
    @Input() public entity: any;

}
