# Conference & Event Venue Platform (SaaS)

Conference & Event Venue Platform is a SaaS product for venues that manage meeting rooms, conference halls, equipment, catering, and bookings in one system. It is designed for venues of different sizes, from small boardroom-focused operators to large multi-space event centers serving clients worldwide.

## Overview

Each venue can configure its own bookable spaces, pricing rules, equipment inventory, catering options, and operational workflows. The platform supports both one-time and recurring events, while helping staff coordinate venue operations, client communication, and invoicing.

The product is intended to reduce booking conflicts, improve service personalization, and give venue teams a single source of truth for event planning and execution.

## Core Capabilities

### Space Management

- Configure spaces ranging from small 10-person boardrooms to 200-person auditoriums
- Define hourly rates and minimum booking durations per space
- Support combinable rooms for venues with removable walls and flexible layouts
- Manage venue-specific availability and booking constraints

### Equipment Management

- Create equipment packages such as AV, recording, and video conferencing
- Set separate pricing for equipment and add-ons
- Track inventory constraints across simultaneous bookings
- Detect conflicts when demand exceeds available equipment inventory

Example: if 5 rooms require projectors but the venue only has 4, the platform flags the conflict before the booking is finalized.

### Catering Management

- Integrate catering through configurable partner relationships
- Offer options from coffee service to full-day catering packages
- Track dietary preferences and attendee-specific catering needs
- Enforce configurable catering order lock times, with a default lead time of 72 hours

### Booking Management

- Create and manage event bookings across spaces, equipment, and catering
- Support recurring event templates such as monthly board meetings and quarterly training sessions
- Replicate event templates with date adjustments
- Store client history and service notes for personalized venue operations

### Invoicing and Billing

- Generate itemized invoices for venue bookings
- Break down charges by space, equipment, catering, and additional services
- Support split billing across multiple cost centers

## User Roles

### CompanyEmployee

- Handles bookings and day-of event coordination
- Works with clients, schedules, equipment, and catering details

### CompanyManager

- Manages space configuration and pricing
- Maintains room setup, rates, and operational booking rules

### CompanyAdmin

- Manages the venue as a whole
- Oversees partner relationships, business settings, and administration

## Subscription Tiering

- Standard tier supports single-venue management
- Premium tier supports multi-venue management

## Typical Use Cases

- Book a board meeting room with video conferencing and coffee service
- Schedule a quarterly training event in a combinable auditorium setup
- Reuse a recurring event template for a monthly client session
- Detect inventory shortages before confirming an AV-heavy event day
- Split invoice costs between departments or cost centers

## Technology

Backend and admin UI are implemented with ASP.NET Core MVC using standard platform libraries such as EF Core, Identity, and PostgreSQL.

The solution follows layered architecture principles with:

- Domain layer
- DAL layer
- BLL layer
- UI layer

The design emphasizes N-Tier architecture and SOLID principles.

## Goal

The goal of this platform is to help venues run event operations more efficiently, avoid scheduling and inventory conflicts, and deliver a more reliable and personalized customer experience at scale.
