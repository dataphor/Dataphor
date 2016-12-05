import { Injectable } from '@angular/core';
import { Headers, Http, Response } from '@angular/http';
import 'rxjs/add/operator/toPromise';
import { config } from '../index';


@Injectable()
export class APIService {

    constructor(private http: Http) { }


    get(query: string) {
        let headers = new Headers({
            'Content-Type': 'application/json',
            // TODO: Comment back in once we've added localhost to CORS on the service end'
            // Reference: http://stackoverflow.com/questions/18234366/restful-webservice-how-to-set-headers-in-java-to-accept-xmlhttprequest-allowed
            //'FHIR-Service' : fhirServiceUri
        });

        let uri = config.api.serviceBase + '/query/' + encodeURI(query);

        return this.http
            .get(uri, { headers: headers })
            .toPromise()
            .then(res => res.json())
            .catch(this.handleError);
    }

    post(query: string) {
        let headers = new Headers({
            'Content-Type': 'application/json',
            // TODO: Comment back in once we've added localhost to CORS on the service end'
            // Reference: http://stackoverflow.com/questions/18234366/restful-webservice-how-to-set-headers-in-java-to-accept-xmlhttprequest-allowed
            //'FHIR-Service' : fhirServiceUri
        });

        let uri = config.api.serviceBase + '/query/'
            //+ encodeURI(query);

        return this.http
            .post(uri, query, { headers: headers })
            .toPromise()
            .then(res => res.json())
            .catch(this.handleError);
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}