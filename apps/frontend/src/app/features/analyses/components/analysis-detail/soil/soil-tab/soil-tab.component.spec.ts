/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { SoilTabComponent } from './soil-tab.component';

describe('SoilTabComponent', () => {
  let component: SoilTabComponent;
  let fixture: ComponentFixture<SoilTabComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SoilTabComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SoilTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
