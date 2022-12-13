- Comments have been added on the code to facilitate the maintainability by the Hotel’s IT department.

- The QoS is critical, we might suggest having a cluster of Web Servers to provide continuity of service if one server is down. Distribution to multiple areas such as Americas, EMEA & APAC might be a plus to provide a proper service.

The assumed Database is the following:
  - Users
    PK Id bigint
    Email varchar(max)
    
  - Bookings
    PK Id bigint
    FK UserId bigint
    Arrival date
    Departure date

  - RoomAvailabilities
    PK Day date
    FK BookingId bigint NULL

- If the world gets better and more rooms are available, the room notion will be easy to add.

- We also assume that the "register user" API already exists and users exist.

- Even if the API is insecure for the purpose of this exercise, userId is mandatory to update/delete a booking.

- The available endpoints are the following:

  GET booking/search: find the available dates
    minDate: date, the min date of the search
    maxDate: date, the max date of the search

  PUT booking: create/update a booking
    arrival: date, arrival day
    departure: date, departure day
    userId: long, the Id of the user that owns the booking
    bookingid: long (optional), in case of an update, the bookingId the user wants to edit

  DELETE booking: delete a booking
    userId: long, the Id of the user that owns the booking
    bookingid: long, the bookingId the user wants to delete
    
