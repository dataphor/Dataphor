//import { Component, Input, OnInit } from '@angular/core';
//import { FormGroup, FormControl, AbstractControl } from "@angular/forms";
//import { TestForm } from '../../test-form';

//@Component({
//    selector: 'd4-interface',
//    template: require('./interface.component.html'),
//    styles: [require('./interface.component.css')],
//    providers: []
//})
//export class InterfaceComponent implements OnInit {
    
//    @Input() title: string = '';
//    @Input() name: string = '';

//    model: any = TestForm.model.MainColumnMain;

//    submitted = false;
    
//    onSubmit() {
//        this.submitted = true;
        
//    }

//    myGroup: FormGroup;

//    ngOnInit() {

//        let group = this.buildFormGroupFromModel(this.model);

//        //this.myGroup = new FormGroup({
//        //    name: new FormControl('Todd Motto'),
//        //    location: new FormControl('England, UK')
//        //});
//        this.myGroup = new FormGroup(group);

//    }

//    buildFormGroupFromModel(model) {
//        //let group: Array<FormGroupMap<FormControl>>;
//        //let group: Array<Object>;
//        //let group: Object;
//        let group: { [key: string]: AbstractControl } = Object(); 
//        let properties = Object.keys(model);
//        properties.forEach((propertyName) => {
//            //group.push(new Object({ propertyName: new FormControl(model[propertyName]) }));
//            //group.push(new FormGroupMap<FormControl>(propertyName: new FormControl(''));
//            group[propertyName] = new FormControl(model[propertyName]);
//        });
//        return group;
//    }

//}

//export interface FormGroupMap<T extends FormControl> {
//    [key: string]: T;
//}

import {
    Component, Input
} from '@angular/core';

import {
    FormControl, Validators
} from '@angular/forms';

import {
    FormControlContainer
} from '../form.component';



@Component({
    selector: 'd4-interface',
    template: require('./interface.component.html'),
    styles: [require('./interface.component.css')],
    providers: []
})
export class InterfaceComponent {

    @Input() title: string = '';
    @Input() name: string = '';

    //model: any = TestForm.model.MainColumnMain;

    //submitted = false;

    //onSubmit() {
    //    this.submitted = true;

    //}

   

}