document
    .querySelectorAll("form")
    .forEach(form => form.addEventListener("submit", (e) => {
        if (navigator.webdriver == true) {
            e.preventDefault();
        }
        e.submitter.disabled = true;
        e.submitter.classList.add("disabled");
    }));

const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]')
const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl))