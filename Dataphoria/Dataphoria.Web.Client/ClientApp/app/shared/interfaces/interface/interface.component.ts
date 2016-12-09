import { Component, Input, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';

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
    
    form: FormGroup;
    payLoad = '';

    //constructor(private qcs: QuestionControlService) { }
    //ngOnInit() {
    //    this.form = this.qcs.toFormGroup(this.questions);
    //}
    onSubmit() {
        this.payLoad = JSON.stringify(this.form.value);
    }
    

}
