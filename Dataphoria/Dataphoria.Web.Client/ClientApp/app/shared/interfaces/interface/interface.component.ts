import { Component, Input, OnInit } from '@angular/core';

@Component({
    selector: 'd4-interface',
    template: require('./interface.component.html'),
    styles: [require('./interface.component.css')],
    providers: []
})
export class InterfaceComponent implements OnInit {
    
    @Input() title: string = '';
    @Input() name: string = '';

    private model: Object;
    

    ngOnInit() {
        this.model = {
            Name: 'Steve',
            Phone: '5555555555',
            Address: 'Scruff McGruff',
            City: "Chicago",
            State: "Illinois",
            Zip: "60652"
        };
    }
    

}
