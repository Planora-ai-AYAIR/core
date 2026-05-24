import { Component, HostListener, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ROUTES } from '../../config/constants';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'],
})
export class NavbarComponent implements AfterViewInit {
  ROUTES = ROUTES;

  isMenuOpen = false;
  activeLink = 'hero'; // start with hero
  isScrolled = false;

  navLinks = [
    { id: 'hero', label: 'Home' },
    { id: 'how-it-works', label: 'How It Works' },
    { id: 'features', label: 'Features' },
    { id: 'cta', label: 'Get Started' },
  ];

  ngAfterViewInit() {
    // Initial check after DOM is painted
    setTimeout(() => this.updateActiveLink(), 100);
  }

  toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
    document.body.style.overflow = this.isMenuOpen ? 'hidden' : '';
  }

  closeMenu(): void {
    if (this.isMenuOpen) {
      this.isMenuOpen = false;
      document.body.style.overflow = '';
    }
  }

  scrollToSection(sectionId: string) {
    // Close mobile menu if open
    this.closeMenu();

    // Smooth scroll to the section
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    // Update URL fragment without reload
    history.pushState(null, '', `#${sectionId}`);

    // Set active link immediately (also will be corrected by scroll listener)
    this.activeLink = sectionId;
  }

  setActive(link: string): void {
    this.activeLink = link;
  }

  OnLoginClicked(): void {
    window.location.href = ROUTES.signIn;
  }

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.isScrolled = window.scrollY > 20;
    this.updateActiveLink(); // <-- now updates the active link while scrolling
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenu();
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth >= 768) {
      this.closeMenu();
    }
  }

  // Determine which section is currently visible
  private updateActiveLink() {
    const scrollPosition = window.scrollY + 120; // small offset for better feel
    const sections = this.navLinks
      .map((link) => document.getElementById(link.id))
      .filter(Boolean) as HTMLElement[];

    for (let i = sections.length - 1; i >= 0; i--) {
      if (sections[i].offsetTop <= scrollPosition) {
        this.activeLink = this.navLinks[i].id;
        return;
      }
    }
    // If none found (scrolled above hero), keep home
    this.activeLink = 'hero';
  }
}
