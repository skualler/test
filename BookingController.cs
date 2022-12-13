using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace LastHotel.Controllers
{
	[Route("booking")]
	public class BookingController : Controller
	{
		/// <summary>
		/// Represents a row from the Users table
		/// </summary>
		public class User
		{
			/// <summary>
			/// User Id
			/// </summary>
			public long Id { get; set; }

			/// <summary>
			/// User Email
			/// </summary>
			public string Email { get; set; }

			/*Other user infos can be added here*/
		}

		/// <summary>
		/// Represents a row from the Bookings table
		/// </summary>
		public class Booking
		{
			/// <summary>
			/// Booking Id
			/// </summary>
			public long Id { get; set; }

			/// <summary>
			/// Needs to be an existing User Id
			/// </summary>
			public long UserId { get; set; }

			/// <summary>
			/// Booking Arrival Date
			/// </summary>
			public DateTime Arrival { get; set; }

			/// <summary>
			/// Booking Departure Date
			/// </summary>
			public DateTime Departure { get; set; }

			/*If there were multiple rooms, the room Id should be here too*/
		}

		/// <summary>
		/// Represents a row from the Room availabilities table
		/// </summary>
		public class RoomAvailability
		{
			public DateTime Day { get; set; }
			public long? BookingId { get; set; }
		}

		private const int MaxAdvance = 30;//If needs the reservation delay needs to be modified, edit here
		private const int MaxDuration = 3;//If the max duration needs to be modified, edit here

		private static List<RoomAvailability> _roomAvailabilities;
		private static List<Booking> _bookings;

		private static DateTime MinAvailableDate => DateTime.Now.Date.AddDays(1);
		private static DateTime MaxAvailableDate => MinAvailableDate.AddDays(MaxAdvance);

		/// <summary>
		/// For the purpose of the exercice, the User table is this list
		/// </summary>
		private static List<User> Users => new List<User>
		{
			new User{Id = 1, Email = "ddelecroix@gmail.com"},
			new User{Id = 2, Email = "recruteur@alten.ca"}
		};

		/// <summary>
		/// For the purpose of the exercice, the Booking table is this list
		/// </summary>
		private static List<Booking> Bookings
		{
			get
			{
				if (_bookings == null || !_bookings.Any())//We populate if empty
					_bookings = new List<Booking>
						{
							new Booking { Id = 1, UserId = 2, Arrival = new DateTime(2022,12,24), Departure = new DateTime(2022,12,26) },
							new Booking { Id = 2, UserId = 1, Arrival = new DateTime(2023,01,02), Departure = new DateTime(2023,01,04) }
						};
				return _bookings;
			}
		}

		/// <summary>
		/// For the purpose of the exercice, the Room availabilities table is this list
		/// </summary>
		private static List<RoomAvailability> RoomAvailabilities
		{
			get
			{
				if (_roomAvailabilities == null || !_roomAvailabilities.Any())//We populate if empty, for this exercice
				{
					_roomAvailabilities = new List<RoomAvailability>();
					for (var day = MinAvailableDate; day < MaxAvailableDate; day = day.AddDays(1))
						_roomAvailabilities.Add(new RoomAvailability
						{
							Day = day,
							BookingId = Bookings.Where(x => x.Arrival <= day && day <= x.Departure).Select(x => (long?)x.Id).FirstOrDefault()//We link the RoomAvailability to block and release the day when a booking is added 
						});

				}
				return _roomAvailabilities;
			}
		}

		/// <summary>
		/// Returns every Day available between the two dates
		/// </summary>
		/// <param name="minDate">No date before that date</param>
		/// <param name="maxDate">No date after that date</param>
		/// <returns></returns>
		[HttpGet, Route("search")]
		public IActionResult CheckAvailability([FromQuery] DateTime minDate, [FromQuery] DateTime maxDate)
		{
			try
			{
				if (minDate < MinAvailableDate || maxDate < MinAvailableDate)
					throw new Exception("Cannot book in the past.");

				if (minDate >= MaxAvailableDate || maxDate >= MaxAvailableDate)
					throw new Exception($"Reservations after the {MaxAvailableDate:dd/MM/yyyy} are not available yet.");

				var availabilities = RoomAvailabilities
					.Where(x => minDate <= x.Day && x.Day <= maxDate && x.BookingId == null)
					.Select(x => x.Day);

				return Ok(availabilities);//HTTP Status 200 with the list of available dates
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);//Returns the error message with HTTP Error 400
			}
		}

		/// <summary>
		/// Books a new stay or updates an existing one
		/// </summary>
		/// <param name="userId">User Id</param>
		///	<param name="bookingId">Booking Id (for updates)</param>
		/// <param name="arrival">Arrival Date</param>
		/// <param name="departure">Departure Date</param>
		/// <returns></returns>
		[HttpPut, Route("")]
		public IActionResult BookRoom([FromQuery] long userId, [FromQuery] long? bookingId, [FromQuery] DateTime arrival, [FromQuery] DateTime departure)
		{
			try
			{
				if (Users.All(user => user.Id != userId))//Only existing users can book
					throw new Exception("User does not exist");

				Booking booking = null;
				if (bookingId != null)//Update attempt
				{
					booking = Bookings.FirstOrDefault(x => x.UserId == userId && bookingId.Value == x.Id);
					if (booking == null)//We only allow update for the proper user
						throw new Exception("The booking was not found for this user.");
				}

				//We remove potential time part from the dates
				arrival = arrival.Date;
				departure = departure.Date;

				if (departure < arrival)//We don't want incoherent date range
					throw new Exception("Invalid date range.");

				var duration = (departure - arrival).TotalDays;
				if (duration > MaxDuration)
					throw new Exception($"You can only book stays from 1 to {MaxDuration} days.");

				if (RoomAvailabilities.Where(x => arrival <= x.Day && x.Day <= departure).Any(x => x.BookingId != null && x.BookingId.Value != bookingId))//We decline if we have at least one day booked for the selected period
					throw new Exception("At least one of the selected dates is unavailable.");

				if (bookingId == null)
				{
					booking = new Booking
					{
						Id = Bookings.Max(x => x.Id) + 1,
						UserId = userId,
					};
					Bookings.Add(booking);//Inserting booking
				}
				booking.Departure = departure;
				booking.Arrival = arrival;

				RoomAvailabilities
					.Where(x => x.BookingId == bookingId)
					.ToList()
					.ForEach(x =>
					{
						x.BookingId = null;
					});//Releasing old room dates

				RoomAvailabilities
					.Where(x => arrival <= x.Day && x.Day <= departure)
					.ToList()
					.ForEach(x =>
					{
						x.BookingId = booking.Id;
					});//Writing the booking availabilities

				return Ok(booking.Id);//HTTP status 200 with the booking Id
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);//Returns the error message with HTTP Error 400
			}
		}

		/// <summary>
		/// Removes a booking from the system
		/// </summary>
		/// <param name="userId">User Id</param>
		///	<param name="bookingId">Booking Id</param>
		/// <returns></returns>
		[HttpDelete, Route("")]
		public IActionResult DeleteBooking([FromQuery] long userId, [FromQuery] long bookingId)
		{
			try
			{
				if (Users.All(user => user.Id != userId))//Only existing users can book
					throw new Exception("User does not exist");

				Booking booking = Bookings.FirstOrDefault(x => x.UserId == userId && bookingId == x.Id);
				if (booking == null)//We only allow delete for the proper user
					throw new Exception("The booking was not found for this user.");

				RoomAvailabilities
					.Where(x => x.BookingId == bookingId)
					.ToList()
					.ForEach(x =>
					{
						x.BookingId = null;
					});//Releasing dates

				Bookings.Remove(booking);//Removing the booking

				return Ok();//HTTP status 200
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);//Returns the error message with HTTP Error 400
			}
		}
	}
}

