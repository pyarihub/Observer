import { Component } from '@angular/core';
import { Point } from './Maps/Point';
import { MapService } from './Maps/Map.Service';
import { MapBound } from './Maps/MapBound';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})

export class AppComponent  {
  
  points: Point[];
  lng = 0;
  lat = 0;
  currentZoom: number;
  markers: Array<any>=[];
  triggerZoon =12;

  center: google.maps.LatLngLiteral
  options: google.maps.MapOptions = {
    mapTypeId: 'hybrid',
    zoomControl: true,
    scrollwheel: false,
    disableDoubleClickZoom: true,
    maxZoom: 15,
    minZoom: 8,
  }
   constructor(private mapService: MapService) {
   }

   ngOnInit() {
    let point = this.mapService.getInitialMapLocation();
    this.lat = point.lat;
    this.lng = point.lng;
  }
  zoomChange(zoom: number) {
    this.currentZoom = zoom;
  }

  boundsChanged(bound: google.maps.LatLngBounds) {
    if(this.currentZoom > this.triggerZoon){
      var boundJson = bound.toJSON();
      this.searchLocations({
        MinLat : boundJson.south,
        MaxLat : boundJson.north,
        MinLng : boundJson.west,
        MaxLng : boundJson.east
      });
    }
    else{
      this.markers = [];
    }
  }

  searchLocations(mapBound: MapBound) {
    this.mapService.searchLocation( mapBound)
    .subscribe(points => {
      points.map(point => {
        this.markers.push({
          position: {
            lat: point.lat,
            lng: point.lng
          },
          label: {
            color: 'red',
            text: point.lat + ', ' + point.lng
          },
          options: { animation: google.maps.Animation.BOUNCE },
        })
      });
      if(this.markers.length > 0)
      {
        let point = this.markers[0].position;
        this.center = {
          lat: point.lat,
          lng:point.lng,
        }
      }
    })
  }
}

