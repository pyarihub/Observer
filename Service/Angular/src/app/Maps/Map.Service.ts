import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Point } from './Point';
import { MapBound } from './MapBound';

@Injectable({ providedIn: 'root' })

export class MapService {

  private PointesUrl = '';  // URL to web api
  //private PointesUrl = 'https://localhost:44319/Map/Search';  // URL to web api
  
  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
  };

  constructor(
    private http: HttpClient) { }

  /** POST: Search Locations with in the Bound */
  searchLocation(mapBound: MapBound): Observable<Point []> {
    return this.http.post<Point []>(this.PointesUrl, mapBound, this.httpOptions).pipe(
      catchError(this.handleError<Point []>('searchLocation'))
    );
  }
  /// This could come from web api based on user's profile or user's current location.etc
  getInitialMapLocation(): Point {
    return {
      lng :   -74.00164795,
        lat : 40.72424316
    }
  }
  /**
   * Handle Http operation that failed.
   * Let the app continue.
   * @param operation - name of the operation that failed
   * @param result - optional value to return as the observable result
   */
  private handleError<T>(operation = 'operation', result?: T) {
    return (error: any): Observable<T> => {

      // TODO: send the error to remote logging infrastructure
      console.error(error); // log to console instead

      // Let the app keep running by returning an empty result.
      return of(result as T);
    };
  }

}
