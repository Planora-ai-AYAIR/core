/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { ParcelListComponent } from './parcel-list.component';

describe('ParcelListComponent', () => {
  let component: ParcelListComponent;
  let fixture: ComponentFixture<ParcelListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ParcelListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ParcelListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
