import { BrowserModule } from '@angular/platform-browser';
import { NgModule ,CUSTOM_ELEMENTS_SCHEMA  } from '@angular/core';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
//import { GoogleMapsModule } from '@angular/google-maps'
import { HttpClientModule } from '@angular/common/http';
import { AgmCoreModule } from '@agm/core';
@NgModule({
  declarations: [
    AppComponent
  ],
  schemas:  [ CUSTOM_ELEMENTS_SCHEMA ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    //GoogleMapsModule,
    AgmCoreModule.forRoot({
      apiKey: ''
    }),
    HttpClientModule,
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
