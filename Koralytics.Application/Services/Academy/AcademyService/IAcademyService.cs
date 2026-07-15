using Koralytics.Application.DTOs.Academies;

namespace Koralytics.Application.Services.Academy.AcademyService
{
    public interface IAcademyService
    {
        /// <summary>
        /// Creates an Academy record after SuperAdmin approves an AcademyRequest.
        /// Updates the AcademyRequest status to Approved.
        /// </summary>
        Task<AcademyResponseDto> CreateAcademyAsync(CreateAcademyDto dto, int performedByUserId);

        /// <summary>
        /// Updates mutable academy properties: Name, LogoUrl, PrimaryColor, SecondaryColor.
        /// </summary>
        Task<AcademyResponseDto> UpdateAcademyAsync(int academyId, UpdateAcademyDto dto, int performedByUserId);

        /// <summary>
        /// Adds a new physical location to the academy.
        /// Automatically sets IsMain = true when it is the first location added.
        /// </summary>
        Task<AcademyLocationResponseDto> AddLocationAsync(int academyId, AddLocationDto dto, int performedByUserId);

        /// <summary>Gets a single academy by id.</summary>
        Task<AcademyResponseDto> GetAcademyAsync(int academyId);

        /// <summary>Gets all academies.</summary>
        Task<IEnumerable<AcademyResponseDto>> GetAllAcademiesAsync();

        /// <summary>Gets all locations for an academy.</summary>
        Task<IEnumerable<AcademyLocationResponseDto>> GetLocationsAsync(int academyId);

        /// <summary>
        /// Promotes a location to main (sets it as IsMain=true, clears IsMain on all others).
        /// </summary>
        Task SetMainLocationAsync(int academyId, int locationId, int performedByUserId);
    }
}
