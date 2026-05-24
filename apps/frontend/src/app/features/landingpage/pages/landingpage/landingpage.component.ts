import { Component, OnInit } from '@angular/core';
import { HeroComponent } from '../../components/hero/hero.component';
import { HowItWorksComponent } from '../../components/how-it-works/how-it-works.component';
import { FeaturesComponent } from '../../components/features/features.component';
import { CtaComponent } from "../../components/cta/cta.component";

@Component({
  selector: 'app-landingpage',
  imports: [HeroComponent, HowItWorksComponent, FeaturesComponent, CtaComponent],
  templateUrl: './landingpage.component.html',
  styleUrls: ['./landingpage.component.css']
})
export class LandingpageComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
